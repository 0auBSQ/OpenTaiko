using System;
using System.Collections.Generic;

namespace OpenTaiko {
	// ── Generic navigation graph + A* (src/Lua/3D/Pathfinding) ──────────────────────────────────────────
	// A reusable weighted graph for stages that pathfind. Nodes carry a world position; edges carry a weight
	// and a "transit point" (e.g. the doorway/portal midpoint a mover crosses when taking that edge). A* over
	// the nodes yields a list of transit points ending at the goal — i.e. waypoints that route AROUND walls.
	//
	// It's deliberately graph-agnostic: WHAT the nodes/edges represent is the stage's business (descent's
	// navmesh.lua maps sectors->nodes and passable portals->edges; another stage could use a tile grid). This
	// class just stores the graph and runs the search, so the search lives in C# (fast) not re-rolled per stage.
	//
	// Lua usage (descent navmesh.lua):
	//   local g = PATHFIND:NewGraph()
	//   local i = g:AddNode(cx,cy,cz)                       -- 1-based index
	//   g:Link(a, b, weight, px,py,pz)                      -- bidirectional edge through transit point (px,py,pz)
	//   local si, gi = g:NearestNode(ax,ay,az), g:NearestNode(bx,by,bz)
	//   local path = g:FindPath(si, gi, goalX,goalY,goalZ)  -- NavPath or nil
	//   if path then for k=1,path:Count() do local x,z = path:X(k), path:Z(k) ... end end

	public sealed class NavPath {
		private readonly List<double> _x = new(), _y = new(), _z = new();
		internal void Add(double x, double y, double z) { _x.Add(x); _y.Add(y); _z.Add(z); }
		public int Count() => _x.Count;
		public double X(int i) => _x[i - 1];   // 1-based for Lua
		public double Y(int i) => _y[i - 1];
		public double Z(int i) => _z[i - 1];
	}

	public sealed class NavGraph {
		private readonly List<double> _nx = new(), _ny = new(), _nz = new();
		private struct Edge { public int To; public double W, Px, Py, Pz; }
		private readonly List<List<Edge>> _adj = new();

		public void Clear() { _nx.Clear(); _ny.Clear(); _nz.Clear(); _adj.Clear(); }
		public int NodeCount() => _nx.Count;

		// add a node at (x,y,z); returns its 1-based index
		public int AddNode(double x, double y, double z) {
			_nx.Add(x); _ny.Add(y); _nz.Add(z); _adj.Add(new List<Edge>());
			return _nx.Count;
		}
		// directed edge a->b (weight, transit point); 1-based indices
		public void AddEdge(int a, int b, double w, double px, double py, double pz) {
			if (a < 1 || a > _adj.Count || b < 1 || b > _adj.Count) return;
			_adj[a - 1].Add(new Edge { To = b, W = w, Px = px, Py = py, Pz = pz });
		}
		// bidirectional edge through one shared transit point
		public void Link(int a, int b, double w, double px, double py, double pz) {
			AddEdge(a, b, w, px, py, pz); AddEdge(b, a, w, px, py, pz);
		}

		public int NearestNode(double x, double y, double z) {
			int best = 0; double bd = double.MaxValue;
			for (int i = 0; i < _nx.Count; i++) {
				double dx = _nx[i] - x, dy = _ny[i] - y, dz = _nz[i] - z; double d = dx * dx + dy * dy + dz * dz;
				if (d < bd) { bd = d; best = i + 1; }
			}
			return best;
		}

		private double Heur(int n, double gx, double gy, double gz) {
			double dx = _nx[n] - gx, dy = _ny[n] - gy, dz = _nz[n] - gz;
			return Math.Sqrt(dx * dx + dy * dy + dz * dz);
		}

		// A* from node `start` to node `goal`; the returned path is the transit points crossed, then (goalX,goalY,goalZ).
		// Returns null if unreachable / bad indices. start==goal -> just the goal point.
		public NavPath FindPath(int start, int goal, double goalX, double goalY, double goalZ) {
			int n = _nx.Count;
			if (start < 1 || start > n || goal < 1 || goal > n) return null;
			start--; goal--;
			if (start == goal) { var p0 = new NavPath(); p0.Add(goalX, goalY, goalZ); return p0; }

			double gx = _nx[goal], gy = _ny[goal], gz = _nz[goal];
			var g = new double[n]; var came = new int[n]; var viaX = new double[n]; var viaY = new double[n]; var viaZ = new double[n];
			var inOpen = new bool[n]; var closed = new bool[n];
			for (int i = 0; i < n; i++) { g[i] = double.MaxValue; came[i] = -1; }
			g[start] = 0; inOpen[start] = true;

			while (true) {
				// pick the open node with the lowest f (linear scan — graphs here are small)
				int cur = -1; double bestF = double.MaxValue;
				for (int i = 0; i < n; i++) if (inOpen[i]) { double f = g[i] + Heur(i, gx, gy, gz); if (f < bestF) { bestF = f; cur = i; } }
				if (cur < 0) return null;                 // open set empty -> unreachable
				if (cur == goal) break;
				inOpen[cur] = false; closed[cur] = true;
				foreach (var e in _adj[cur]) {
					int to = e.To - 1; if (closed[to]) continue;
					double ng = g[cur] + e.W;
					if (ng < g[to]) { g[to] = ng; came[to] = cur; viaX[to] = e.Px; viaY[to] = e.Py; viaZ[to] = e.Pz; inOpen[to] = true; }
				}
			}

			// reconstruct: collect the transit points from goal back to start, reverse, append the goal point
			var stack = new List<(double, double, double)>();
			int node = goal;
			while (came[node] != -1) { stack.Add((viaX[node], viaY[node], viaZ[node])); node = came[node]; }
			var path = new NavPath();
			for (int i = stack.Count - 1; i >= 0; i--) path.Add(stack[i].Item1, stack[i].Item2, stack[i].Item3);
			path.Add(goalX, goalY, goalZ);
			return path;
		}
	}

	// Lua factory: PATHFIND:NewGraph()
	public sealed class LuaPathfindFunc {
		public NavGraph NewGraph() => new NavGraph();
	}
}
