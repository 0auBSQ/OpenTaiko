<!-- api/math.md -->

# 数学

ベクトル、行列、クォータニオン。

各型は、値を作成するファクトリグローバル (VECTOR2、MATRIX4、QUATERNION など) と、演算を持つハンドル型の組で提供されます。このページのすべての型に共通する規約:

- 成分フィールド (X、Y、Z、W) は直接読み書きしてください。インデックスアクセス (`Get`、`Set`) は 1 始まりです。
- 算術メソッドは新しい値を返し、オペランドを変更しません。`Set` だけがその場で値を変更します。
- 角度はラジアンです。
- 複数の数値を返すメソッド (`Unpack`、`RotateVec`) は、複数の Lua 値として返します。

```lua
local a = VECTOR2:CreateVector2(3, 4)
local b = a:Normalized():Scale(10)   -- (6, 8)
local x, y = b:Unpack()

local q = QUATERNION:FromAxisAngle(0, 1, 0, math.pi / 2)
local rx, ry, rz = q:RotateVec(1, 0, 0)
```

## ベクトル

### VECTOR

任意の長さのベクトルのファクトリです。

<div class="callout warn">
グローバル VECTOR として利用できます。2 つのベクトル間の演算は同じサイズを必要とします。サイズが一致しない場合、長さ 0 のベクトル (スカラー結果では 0) を返し、エラーは発生させません。
</div>

| メソッド | 説明 |
| --- | --- |
| `VECTOR:CreateVector(n)  -> vector` | 長さ n のゼロで埋められたベクトルを作成します。 |

### ベクトルハンドル

1 始まりの成分を持つ任意の長さのベクトルです。

| メソッド | 説明 |
| --- | --- |
| `vector:Size()  -> number` | 成分の数を返します。 |
| `vector:Get(i)  -> number` | i 番目の成分を返します。i が範囲外なら 0。 |
| `vector:Set(i, value)  -> nil` | i 番目の成分を設定します。このメソッドは範囲外のインデックスを無視します。 |
| `vector:Add(o)  -> vector` | 成分ごとの和を返します。 |
| `vector:Sub(o)  -> vector` | 成分ごとの差を返します。 |
| `vector:Mul(o)  -> vector` | 成分ごとの積を返します。 |
| `vector:Scale(s)  -> vector` | すべての成分に s を掛けたベクトルを返します。 |
| `vector:Dot(o)  -> number` | 内積を返します。 |
| `vector:Length()  -> number` | ユークリッド長を返します。 |
| `vector:LengthSq()  -> number` | 長さの 2 乗を返します。 |
| `vector:Distance(o)  -> number` | o までのユークリッド距離を返します。 |
| `vector:Normalized()  -> vector` | 単位長のコピーを返します。長さがほぼ 0 ならゼロベクトル。 |
| `vector:Lerp(o, t)  -> vector` | o に向かう割合 t の線形補間を返します。 |
| `vector:Clone()  -> vector` | 独立したコピーを返します。 |

### VECTOR2

2D ベクトルのファクトリです。

<div class="callout warn">
グローバル VECTOR2 として利用できます。
</div>

| メソッド | 説明 |
| --- | --- |
| `VECTOR2:CreateVector2(x, y)  -> vector2` | 2D ベクトルを作成します。 |
| `VECTOR2:Zero()  -> vector2` | (0, 0) を返します。 |
| `VECTOR2:One()  -> vector2` | (1, 1) を返します。 |

### Vector2 ハンドル

X と Y フィールドを持つ 2D ベクトルです。

| メソッド | 説明 |
| --- | --- |
| `vector2.X  -> number` | x 成分 (読み書き可能なフィールド)。 |
| `vector2.Y  -> number` | y 成分 (読み書き可能なフィールド)。 |
| `vector2:Add(o)  -> vector2` | 成分ごとの和を返します。 |
| `vector2:Sub(o)  -> vector2` | 成分ごとの差を返します。 |
| `vector2:Mul(o)  -> vector2` | 成分ごとの積を返します。 |
| `vector2:Scale(s)  -> vector2` | s を掛けたベクトルを返します。 |
| `vector2:Negate()  -> vector2` | (-X, -Y) を返します。 |
| `vector2:Dot(o)  -> number` | 内積を返します。 |
| `vector2:Cross(o)  -> number` | スカラーの外積 (X * o.Y - Y * o.X) を返します。 |
| `vector2:Length()  -> number` | 長さを返します。 |
| `vector2:LengthSq()  -> number` | 長さの 2 乗を返します。 |
| `vector2:Distance(o)  -> number` | o までの距離を返します。 |
| `vector2:Normalized()  -> vector2` | 単位長のコピーを返します。長さがほぼ 0 なら (0, 0)。 |
| `vector2:Lerp(o, t)  -> vector2` | o に向かう割合 t の線形補間を返します。 |
| `vector2:Rotate(radians)  -> vector2` | 指定した角度だけ反時計回りに回転したベクトルを返します。 |
| `vector2:Clone()  -> vector2` | 独立したコピーを返します。 |
| `vector2:Set(x, y)  -> nil` | X と Y をその場で設定します。 |
| `vector2:Unpack()  -> number, number` | X と Y を 2 つの値として返します。 |
| `vector2:ToString()  -> string` | "(X, Y)" を返します。 |

### VECTOR3

3D ベクトルのファクトリです。

<div class="callout warn">
グローバル VECTOR3 として利用できます。
</div>

| メソッド | 説明 |
| --- | --- |
| `VECTOR3:CreateVector3(x, y, z)  -> vector3` | 3D ベクトルを作成します。 |
| `VECTOR3:Zero()  -> vector3` | (0, 0, 0) を返します。 |
| `VECTOR3:One()  -> vector3` | (1, 1, 1) を返します。 |

### Vector3 ハンドル

X、Y、Z フィールドを持つ 3D ベクトルです。

| メソッド | 説明 |
| --- | --- |
| `vector3.X  -> number` | x 成分 (読み書き可能なフィールド)。 |
| `vector3.Y  -> number` | y 成分 (読み書き可能なフィールド)。 |
| `vector3.Z  -> number` | z 成分 (読み書き可能なフィールド)。 |
| `vector3:Add(o)  -> vector3` | 成分ごとの和を返します。 |
| `vector3:Sub(o)  -> vector3` | 成分ごとの差を返します。 |
| `vector3:Mul(o)  -> vector3` | 成分ごとの積を返します。 |
| `vector3:Scale(s)  -> vector3` | s を掛けたベクトルを返します。 |
| `vector3:Negate()  -> vector3` | (-X, -Y, -Z) を返します。 |
| `vector3:Dot(o)  -> number` | 内積を返します。 |
| `vector3:Cross(o)  -> vector3` | 外積を返します。 |
| `vector3:Length()  -> number` | 長さを返します。 |
| `vector3:LengthSq()  -> number` | 長さの 2 乗を返します。 |
| `vector3:Distance(o)  -> number` | o までの距離を返します。 |
| `vector3:Normalized()  -> vector3` | 単位長のコピーを返します。長さがほぼ 0 なら (0, 0, 0)。 |
| `vector3:Lerp(o, t)  -> vector3` | o に向かう割合 t の線形補間を返します。 |
| `vector3:Reflect(n)  -> vector3` | 法線 n について反射したベクトルを返します (n には単位長のベクトルを渡してください)。 |
| `vector3:Clone()  -> vector3` | 独立したコピーを返します。 |
| `vector3:Set(x, y, z)  -> nil` | X、Y、Z をその場で設定します。 |
| `vector3:Unpack()  -> number, number, number` | X、Y、Z を 3 つの値として返します。 |
| `vector3:ToString()  -> string` | "(X, Y, Z)" を返します。 |

### VECTOR4

4D ベクトルのファクトリです。

<div class="callout warn">
グローバル VECTOR4 として利用できます。
</div>

| メソッド | 説明 |
| --- | --- |
| `VECTOR4:CreateVector4(x, y, z, w)  -> vector4` | 4D ベクトルを作成します。 |
| `VECTOR4:Zero()  -> vector4` | (0, 0, 0, 0) を返します。 |
| `VECTOR4:One()  -> vector4` | (1, 1, 1, 1) を返します。 |

### Vector4 ハンドル

X、Y、Z、W フィールドを持つ 4D ベクトルです。

| メソッド | 説明 |
| --- | --- |
| `vector4.X  -> number` | x 成分 (読み書き可能なフィールド)。 |
| `vector4.Y  -> number` | y 成分 (読み書き可能なフィールド)。 |
| `vector4.Z  -> number` | z 成分 (読み書き可能なフィールド)。 |
| `vector4.W  -> number` | w 成分 (読み書き可能なフィールド)。 |
| `vector4:Add(o)  -> vector4` | 成分ごとの和を返します。 |
| `vector4:Sub(o)  -> vector4` | 成分ごとの差を返します。 |
| `vector4:Mul(o)  -> vector4` | 成分ごとの積を返します。 |
| `vector4:Scale(s)  -> vector4` | s を掛けたベクトルを返します。 |
| `vector4:Negate()  -> vector4` | (-X, -Y, -Z, -W) を返します。 |
| `vector4:Dot(o)  -> number` | 内積を返します。 |
| `vector4:Length()  -> number` | 長さを返します。 |
| `vector4:LengthSq()  -> number` | 長さの 2 乗を返します。 |
| `vector4:Distance(o)  -> number` | o までの距離を返します。 |
| `vector4:Normalized()  -> vector4` | 単位長のコピーを返します。長さがほぼ 0 なら (0, 0, 0, 0)。 |
| `vector4:Lerp(o, t)  -> vector4` | o に向かう割合 t の線形補間を返します。 |
| `vector4:Clone()  -> vector4` | 独立したコピーを返します。 |
| `vector4:Set(x, y, z, w)  -> nil` | X、Y、Z、W をその場で設定します。 |
| `vector4:Unpack()  -> number, number, number, number` | X、Y、Z、W を 4 つの値として返します。 |
| `vector4:ToString()  -> string` | "(X, Y, Z, W)" を返します。 |

## 行列

すべての行列は行優先です。`Get(r, c)` は行 r、列 c を読み取り、どちらも 1 始まりです。`MulVec` はベクトルを列ベクトルとして扱います (結果 = M * v)。

### MATRIX

任意のサイズの行列のファクトリです。

<div class="callout warn">
グローバル MATRIX として利用できます。サイズが適合しない演算は空の 0 x 0 行列 (または長さ 0 のベクトル) を返し、エラーは発生させません。
</div>

| メソッド | 説明 |
| --- | --- |
| `MATRIX:CreateMatrix(rows, cols)  -> matrix` | ゼロで埋められた行列を作成します。 |
| `MATRIX:Identity(n)  -> matrix` | n x n の単位行列を作成します。 |

### 行列ハンドル

任意のサイズの行列です。

<div class="callout warn">
Add と Sub は次元の一致を必要とします。Mul はこの行列の列数が相手の行数と等しいことを必要とします。MulVec はベクトルのサイズが列数と等しいことを必要とします。
</div>

| メソッド | 説明 |
| --- | --- |
| `matrix:RowCount()  -> number` | 行数を返します。 |
| `matrix:ColCount()  -> number` | 列数を返します。 |
| `matrix:Get(r, c)  -> number` | 行 r、列 c の要素を返します。範囲外なら 0。 |
| `matrix:Set(r, c, v)  -> nil` | 行 r、列 c の要素を設定します。このメソッドは範囲外のインデックスを無視します。 |
| `matrix:Add(o)  -> matrix` | 要素ごとの和を返します。 |
| `matrix:Sub(o)  -> matrix` | 要素ごとの差を返します。 |
| `matrix:Scale(s)  -> matrix` | すべての要素に s を掛けた行列を返します。 |
| `matrix:Mul(o)  -> matrix` | 行列の積を返します。 |
| `matrix:MulVec(v)  -> vector` | ベクトルとの積を返します (VECTOR を参照)。 |
| `matrix:Transpose()  -> matrix` | 転置行列を返します。 |
| `matrix:Clone()  -> matrix` | 独立したコピーを返します。 |

### MATRIX2

2 x 2 行列のファクトリです。

<div class="callout warn">
グローバル MATRIX2 として利用できます。Get と Set はインデックスを検査しません。1 から 2 の範囲に収めてください。
</div>

| メソッド | 説明 |
| --- | --- |
| `MATRIX2:Identity()  -> matrix2` | 単位行列を返します。 |
| `MATRIX2:Create(m11, m12, m21, m22)  -> matrix2` | 行優先の順で 4 つの要素から行列を作成します。 |
| `MATRIX2:Rotation(radians)  -> matrix2` | 反時計回りの回転行列を返します。 |
| `MATRIX2:Scaling(sx, sy)  -> matrix2` | 拡大縮小行列を返します。 |

### Matrix2 ハンドル

2 x 2 行列です。

| メソッド | 説明 |
| --- | --- |
| `matrix2:Get(r, c)  -> number` | 行 r、列 c の要素を返します。 |
| `matrix2:Set(r, c, v)  -> nil` | 行 r、列 c の要素を設定します。 |
| `matrix2:Mul(o)  -> matrix2` | 行列の積を返します。 |
| `matrix2:MulVec(v)  -> vector2` | 2D ベクトルとの積を返します。 |
| `matrix2:Scale(s)  -> matrix2` | すべての要素に s を掛けた行列を返します。 |
| `matrix2:Transpose()  -> matrix2` | 転置行列を返します。 |
| `matrix2:Determinant()  -> number` | 行列式を返します。 |
| `matrix2:Clone()  -> matrix2` | 独立したコピーを返します。 |

### MATRIX3

3 x 3 行列のファクトリです。

<div class="callout warn">
グローバル MATRIX3 として利用できます。Get と Set はインデックスを検査しません。1 から 3 の範囲に収めてください。
</div>

| メソッド | 説明 |
| --- | --- |
| `MATRIX3:Identity()  -> matrix3` | 単位行列を返します。 |
| `MATRIX3:Create(m11, m12, m13, m21, m22, m23, m31, m32, m33)  -> matrix3` | 行優先の順で 9 つの要素から行列を作成します。 |
| `MATRIX3:YawPitchRoll(yaw, pitch, roll)  -> matrix3` | ヨー、ピッチ、ロールの角度から作られた回転行列を返します。 |

### Matrix3 ハンドル

3 x 3 行列です。

| メソッド | 説明 |
| --- | --- |
| `matrix3:Get(r, c)  -> number` | 行 r、列 c の要素を返します。 |
| `matrix3:Set(r, c, v)  -> nil` | 行 r、列 c の要素を設定します。 |
| `matrix3:Mul(o)  -> matrix3` | 行列の積を返します。 |
| `matrix3:MulVec(v)  -> vector3` | 3D ベクトルとの積を返します。 |
| `matrix3:Scale(s)  -> matrix3` | すべての要素に s を掛けた行列を返します。 |
| `matrix3:Transpose()  -> matrix3` | 転置行列を返します。 |
| `matrix3:Determinant()  -> number` | 行列式を返します。 |
| `matrix3:Clone()  -> matrix3` | 独立したコピーを返します。 |

### MATRIX4

4 x 4 行列のファクトリです。

<div class="callout warn">
グローバル MATRIX4 として利用できます。Get と Set はインデックスを検査しません。1 から 4 の範囲に収めてください。Translation はオフセットを第 4 列に格納するため、MulVec を通じて W = 1 の vector4 に適用されます。
</div>

| メソッド | 説明 |
| --- | --- |
| `MATRIX4:Identity()  -> matrix4` | 単位行列を返します。 |
| `MATRIX4:Translation(x, y, z)  -> matrix4` | 平行移動行列を返します。 |
| `MATRIX4:Scaling(x, y, z)  -> matrix4` | 拡大縮小行列を返します。 |

### Matrix4 ハンドル

4 x 4 行列です。

| メソッド | 説明 |
| --- | --- |
| `matrix4:Get(r, c)  -> number` | 行 r、列 c の要素を返します。 |
| `matrix4:Set(r, c, v)  -> nil` | 行 r、列 c の要素を設定します。 |
| `matrix4:Mul(o)  -> matrix4` | 行列の積を返します。 |
| `matrix4:MulVec(v)  -> vector4` | 4D ベクトルとの積を返します。 |
| `matrix4:Scale(s)  -> matrix4` | すべての要素に s を掛けた行列を返します。 |
| `matrix4:Transpose()  -> matrix4` | 転置行列を返します。 |
| `matrix4:Clone()  -> matrix4` | 独立したコピーを返します。 |

## クォータニオン

### QUATERNION

クォータニオンのファクトリです。

<div class="callout warn">
グローバル QUATERNION として利用できます。単位元は (0, 0, 0, 1) です。回転メソッドは単位クォータニオンを前提とします。手作業で組み立てた後は Normalized を呼んでください。
</div>

| メソッド | 説明 |
| --- | --- |
| `QUATERNION:Identity()  -> quaternion` | 単位クォータニオンを返します。 |
| `QUATERNION:Create(x, y, z, w)  -> quaternion` | 4 つの成分からクォータニオンを作成します。 |
| `QUATERNION:FromAxisAngle(ax, ay, az, angle)  -> quaternion` | 軸 (ax, ay, az) の周りの `angle` ラジアンの回転を返します。このメソッドは軸を正規化します。ゼロ軸は単位元になります。 |

### クォータニオンハンドル

X、Y、Z、W フィールドを持つクォータニオンです。

| メソッド | 説明 |
| --- | --- |
| `quaternion.X  -> number` | x 成分 (読み書き可能なフィールド)。 |
| `quaternion.Y  -> number` | y 成分 (読み書き可能なフィールド)。 |
| `quaternion.Z  -> number` | z 成分 (読み書き可能なフィールド)。 |
| `quaternion.W  -> number` | w 成分 (読み書き可能なフィールド)。 |
| `quaternion:Mul(o)  -> quaternion` | ハミルトン積 this * o を返します。結果を適用すると、まず o、次に this で回転します。 |
| `quaternion:Dot(o)  -> number` | 内積を返します。 |
| `quaternion:Length()  -> number` | 長さを返します。 |
| `quaternion:Normalized()  -> quaternion` | 単位長のコピーを返します。長さがほぼ 0 なら単位元。 |
| `quaternion:Conjugate()  -> quaternion` | (-X, -Y, -Z, W)、つまり単位クォータニオンの逆回転を返します。 |
| `quaternion:RotateVec(vx, vy, vz)  -> number, number, number` | ベクトル (vx, vy, vz) を回転し、結果の x、y、z を返します。 |
| `quaternion:Slerp(o, t)  -> quaternion` | 最短の弧を取って、o に向かう割合 t の球面線形補間を返します。 |
| `quaternion:Clone()  -> quaternion` | 独立したコピーを返します。 |
| `quaternion:Unpack()  -> number, number, number, number` | X、Y、Z、W を 4 つの値として返します。 |
