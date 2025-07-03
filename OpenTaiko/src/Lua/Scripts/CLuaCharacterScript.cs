using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko {
	public class CLuaCharacterScript {
		private static string backup_script = """
			function init()
				EVENT:Subscribe("Legacy_DrawRequested")
			end

			function update()
			end

			function draw()
			end

			function TriggerEvent(string, lua_event)
				if string == "Legacy_DrawRequested" then
					lua_event.Texture:DrawAtAnchor(lua_event.X, lua_event.Y, lua_event.Anchor)
				end
			end
			""";
	}
}
