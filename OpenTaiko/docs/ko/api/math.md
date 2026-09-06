<!-- api/math.md -->

# 수학

벡터, 행렬, 쿼터니언.

각 타입은 값을 만드는 팩토리 전역(VECTOR2, MATRIX4, QUATERNION, ...)과 연산을 가진 핸들 타입으로 제공됩니다. 이 페이지의 모든 타입이 공유하는 규약:

- 성분 필드(X, Y, Z, W)는 직접 읽고 쓰십시오. 인덱스 접근(`Get`, `Set`)은 1부터 시작합니다.
- 산술 메서드는 새 값을 반환하고 피연산자는 건드리지 않습니다. `Set`만 값을 제자리에서 수정합니다.
- 각도는 라디안입니다.
- 여러 숫자를 반환하는 메서드(`Unpack`, `RotateVec`)는 여러 Lua 값으로 반환합니다.

```lua
local a = VECTOR2:CreateVector2(3, 4)
local b = a:Normalized():Scale(10)   -- (6, 8)
local x, y = b:Unpack()

local q = QUATERNION:FromAxisAngle(0, 1, 0, math.pi / 2)
local rx, ry, rz = q:RotateVec(1, 0, 0)
```

## 벡터

### VECTOR

임의 길이 벡터의 팩토리입니다.

<div class="callout warn">
전역 VECTOR로 사용할 수 있습니다. 두 벡터 사이의 연산은 크기가 같아야 합니다. 크기가 맞지 않으면 길이 0의 벡터(스칼라 결과는 0)를 반환하며 오류를 내지 않습니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `VECTOR:CreateVector(n)  -> vector` | 길이 n의 0으로 채워진 벡터를 만듭니다. |

### 벡터 핸들

1부터 인덱싱하는 성분을 가진 임의 길이의 벡터입니다.

| 메서드 | 설명 |
| --- | --- |
| `vector:Size()  -> number` | 성분 수를 반환합니다. |
| `vector:Get(i)  -> number` | i번째 성분을 반환하며, i가 범위를 벗어나면 0. |
| `vector:Set(i, value)  -> nil` | i번째 성분을 설정합니다. 범위 밖 인덱스는 이 메서드가 무시합니다. |
| `vector:Add(o)  -> vector` | 성분별 합을 반환합니다. |
| `vector:Sub(o)  -> vector` | 성분별 차를 반환합니다. |
| `vector:Mul(o)  -> vector` | 성분별 곱을 반환합니다. |
| `vector:Scale(s)  -> vector` | 모든 성분에 s를 곱한 벡터를 반환합니다. |
| `vector:Dot(o)  -> number` | 내적을 반환합니다. |
| `vector:Length()  -> number` | 유클리드 길이를 반환합니다. |
| `vector:LengthSq()  -> number` | 길이의 제곱을 반환합니다. |
| `vector:Distance(o)  -> number` | o까지의 유클리드 거리를 반환합니다. |
| `vector:Normalized()  -> vector` | 단위 길이 사본을 반환하며, 길이가 0에 가까우면 0 벡터. |
| `vector:Lerp(o, t)  -> vector` | 비율 t만큼 o를 향한 선형 보간을 반환합니다. |
| `vector:Clone()  -> vector` | 독립된 사본을 반환합니다. |

### VECTOR2

2D 벡터의 팩토리입니다.

<div class="callout warn">
전역 VECTOR2로 사용할 수 있습니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `VECTOR2:CreateVector2(x, y)  -> vector2` | 2D 벡터를 만듭니다. |
| `VECTOR2:Zero()  -> vector2` | (0, 0)을 반환합니다. |
| `VECTOR2:One()  -> vector2` | (1, 1)을 반환합니다. |

### Vector2 핸들

X와 Y 필드를 가진 2D 벡터입니다.

| 메서드 | 설명 |
| --- | --- |
| `vector2.X  -> number` | x 성분(읽고 쓸 수 있는 필드). |
| `vector2.Y  -> number` | y 성분(읽고 쓸 수 있는 필드). |
| `vector2:Add(o)  -> vector2` | 성분별 합을 반환합니다. |
| `vector2:Sub(o)  -> vector2` | 성분별 차를 반환합니다. |
| `vector2:Mul(o)  -> vector2` | 성분별 곱을 반환합니다. |
| `vector2:Scale(s)  -> vector2` | s를 곱한 벡터를 반환합니다. |
| `vector2:Negate()  -> vector2` | (-X, -Y)를 반환합니다. |
| `vector2:Dot(o)  -> number` | 내적을 반환합니다. |
| `vector2:Cross(o)  -> number` | 스칼라 외적(X * o.Y - Y * o.X)을 반환합니다. |
| `vector2:Length()  -> number` | 길이를 반환합니다. |
| `vector2:LengthSq()  -> number` | 길이의 제곱을 반환합니다. |
| `vector2:Distance(o)  -> number` | o까지의 거리를 반환합니다. |
| `vector2:Normalized()  -> vector2` | 단위 길이 사본을 반환하며, 길이가 0에 가까우면 (0, 0). |
| `vector2:Lerp(o, t)  -> vector2` | 비율 t만큼 o를 향한 선형 보간을 반환합니다. |
| `vector2:Rotate(radians)  -> vector2` | 지정한 각도만큼 반시계 방향으로 회전한 벡터를 반환합니다. |
| `vector2:Clone()  -> vector2` | 독립된 사본을 반환합니다. |
| `vector2:Set(x, y)  -> nil` | X와 Y를 제자리에서 설정합니다. |
| `vector2:Unpack()  -> number, number` | X와 Y를 두 값으로 반환합니다. |
| `vector2:ToString()  -> string` | "(X, Y)"를 반환합니다. |

### VECTOR3

3D 벡터의 팩토리입니다.

<div class="callout warn">
전역 VECTOR3로 사용할 수 있습니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `VECTOR3:CreateVector3(x, y, z)  -> vector3` | 3D 벡터를 만듭니다. |
| `VECTOR3:Zero()  -> vector3` | (0, 0, 0)을 반환합니다. |
| `VECTOR3:One()  -> vector3` | (1, 1, 1)을 반환합니다. |

### Vector3 핸들

X, Y, Z 필드를 가진 3D 벡터입니다.

| 메서드 | 설명 |
| --- | --- |
| `vector3.X  -> number` | x 성분(읽고 쓸 수 있는 필드). |
| `vector3.Y  -> number` | y 성분(읽고 쓸 수 있는 필드). |
| `vector3.Z  -> number` | z 성분(읽고 쓸 수 있는 필드). |
| `vector3:Add(o)  -> vector3` | 성분별 합을 반환합니다. |
| `vector3:Sub(o)  -> vector3` | 성분별 차를 반환합니다. |
| `vector3:Mul(o)  -> vector3` | 성분별 곱을 반환합니다. |
| `vector3:Scale(s)  -> vector3` | s를 곱한 벡터를 반환합니다. |
| `vector3:Negate()  -> vector3` | (-X, -Y, -Z)를 반환합니다. |
| `vector3:Dot(o)  -> number` | 내적을 반환합니다. |
| `vector3:Cross(o)  -> vector3` | 외적을 반환합니다. |
| `vector3:Length()  -> number` | 길이를 반환합니다. |
| `vector3:LengthSq()  -> number` | 길이의 제곱을 반환합니다. |
| `vector3:Distance(o)  -> number` | o까지의 거리를 반환합니다. |
| `vector3:Normalized()  -> vector3` | 단위 길이 사본을 반환하며, 길이가 0에 가까우면 (0, 0, 0). |
| `vector3:Lerp(o, t)  -> vector3` | 비율 t만큼 o를 향한 선형 보간을 반환합니다. |
| `vector3:Reflect(n)  -> vector3` | 법선 n에 대해 반사한 벡터를 반환합니다(단위 길이의 n을 넘기십시오). |
| `vector3:Clone()  -> vector3` | 독립된 사본을 반환합니다. |
| `vector3:Set(x, y, z)  -> nil` | X, Y, Z를 제자리에서 설정합니다. |
| `vector3:Unpack()  -> number, number, number` | X, Y, Z를 세 값으로 반환합니다. |
| `vector3:ToString()  -> string` | "(X, Y, Z)"를 반환합니다. |

### VECTOR4

4D 벡터의 팩토리입니다.

<div class="callout warn">
전역 VECTOR4로 사용할 수 있습니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `VECTOR4:CreateVector4(x, y, z, w)  -> vector4` | 4D 벡터를 만듭니다. |
| `VECTOR4:Zero()  -> vector4` | (0, 0, 0, 0)을 반환합니다. |
| `VECTOR4:One()  -> vector4` | (1, 1, 1, 1)을 반환합니다. |

### Vector4 핸들

X, Y, Z, W 필드를 가진 4D 벡터입니다.

| 메서드 | 설명 |
| --- | --- |
| `vector4.X  -> number` | x 성분(읽고 쓸 수 있는 필드). |
| `vector4.Y  -> number` | y 성분(읽고 쓸 수 있는 필드). |
| `vector4.Z  -> number` | z 성분(읽고 쓸 수 있는 필드). |
| `vector4.W  -> number` | w 성분(읽고 쓸 수 있는 필드). |
| `vector4:Add(o)  -> vector4` | 성분별 합을 반환합니다. |
| `vector4:Sub(o)  -> vector4` | 성분별 차를 반환합니다. |
| `vector4:Mul(o)  -> vector4` | 성분별 곱을 반환합니다. |
| `vector4:Scale(s)  -> vector4` | s를 곱한 벡터를 반환합니다. |
| `vector4:Negate()  -> vector4` | (-X, -Y, -Z, -W)를 반환합니다. |
| `vector4:Dot(o)  -> number` | 내적을 반환합니다. |
| `vector4:Length()  -> number` | 길이를 반환합니다. |
| `vector4:LengthSq()  -> number` | 길이의 제곱을 반환합니다. |
| `vector4:Distance(o)  -> number` | o까지의 거리를 반환합니다. |
| `vector4:Normalized()  -> vector4` | 단위 길이 사본을 반환하며, 길이가 0에 가까우면 (0, 0, 0, 0). |
| `vector4:Lerp(o, t)  -> vector4` | 비율 t만큼 o를 향한 선형 보간을 반환합니다. |
| `vector4:Clone()  -> vector4` | 독립된 사본을 반환합니다. |
| `vector4:Set(x, y, z, w)  -> nil` | X, Y, Z, W를 제자리에서 설정합니다. |
| `vector4:Unpack()  -> number, number, number, number` | X, Y, Z, W를 네 값으로 반환합니다. |
| `vector4:ToString()  -> string` | "(X, Y, Z, W)"를 반환합니다. |

## 행렬

모든 행렬은 행 우선입니다. `Get(r, c)`는 r행 c열을 읽으며 둘 다 1부터 시작합니다. `MulVec`는 벡터를 열 벡터로 취급합니다(결과 = M * v).

### MATRIX

임의 크기 행렬의 팩토리입니다.

<div class="callout warn">
전역 MATRIX로 사용할 수 있습니다. 크기가 호환되지 않는 연산은 빈 0 x 0 행렬(또는 길이 0의 벡터)을 반환하며 오류를 내지 않습니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `MATRIX:CreateMatrix(rows, cols)  -> matrix` | 0으로 채워진 행렬을 만듭니다. |
| `MATRIX:Identity(n)  -> matrix` | n x n 단위 행렬을 만듭니다. |

### 행렬 핸들

임의 크기의 행렬입니다.

<div class="callout warn">
Add와 Sub는 차원이 일치해야 합니다. Mul은 이 행렬의 열 수가 상대의 행 수와 같아야 합니다. MulVec는 벡터 크기가 열 수와 같아야 합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `matrix:RowCount()  -> number` | 행 수를 반환합니다. |
| `matrix:ColCount()  -> number` | 열 수를 반환합니다. |
| `matrix:Get(r, c)  -> number` | r행 c열의 원소를 반환하며, 범위를 벗어나면 0. |
| `matrix:Set(r, c, v)  -> nil` | r행 c열의 원소를 설정합니다. 범위 밖 인덱스는 이 메서드가 무시합니다. |
| `matrix:Add(o)  -> matrix` | 원소별 합을 반환합니다. |
| `matrix:Sub(o)  -> matrix` | 원소별 차를 반환합니다. |
| `matrix:Scale(s)  -> matrix` | 모든 원소에 s를 곱한 행렬을 반환합니다. |
| `matrix:Mul(o)  -> matrix` | 행렬 곱을 반환합니다. |
| `matrix:MulVec(v)  -> vector` | 벡터와의 곱을 반환합니다(VECTOR 참고). |
| `matrix:Transpose()  -> matrix` | 전치 행렬을 반환합니다. |
| `matrix:Clone()  -> matrix` | 독립된 사본을 반환합니다. |

### MATRIX2

2 x 2 행렬의 팩토리입니다.

<div class="callout warn">
전역 MATRIX2로 사용할 수 있습니다. Get과 Set은 인덱스를 검사하지 않으므로 1에서 2 사이로 유지하십시오.
</div>

| 메서드 | 설명 |
| --- | --- |
| `MATRIX2:Identity()  -> matrix2` | 단위 행렬을 반환합니다. |
| `MATRIX2:Create(m11, m12, m21, m22)  -> matrix2` | 행 우선 순서의 네 원소로 행렬을 만듭니다. |
| `MATRIX2:Rotation(radians)  -> matrix2` | 반시계 방향 회전 행렬을 반환합니다. |
| `MATRIX2:Scaling(sx, sy)  -> matrix2` | 스케일 행렬을 반환합니다. |

### Matrix2 핸들

2 x 2 행렬입니다.

| 메서드 | 설명 |
| --- | --- |
| `matrix2:Get(r, c)  -> number` | r행 c열의 원소를 반환합니다. |
| `matrix2:Set(r, c, v)  -> nil` | r행 c열의 원소를 설정합니다. |
| `matrix2:Mul(o)  -> matrix2` | 행렬 곱을 반환합니다. |
| `matrix2:MulVec(v)  -> vector2` | 2D 벡터와의 곱을 반환합니다. |
| `matrix2:Scale(s)  -> matrix2` | 모든 원소에 s를 곱한 행렬을 반환합니다. |
| `matrix2:Transpose()  -> matrix2` | 전치 행렬을 반환합니다. |
| `matrix2:Determinant()  -> number` | 행렬식을 반환합니다. |
| `matrix2:Clone()  -> matrix2` | 독립된 사본을 반환합니다. |

### MATRIX3

3 x 3 행렬의 팩토리입니다.

<div class="callout warn">
전역 MATRIX3로 사용할 수 있습니다. Get과 Set은 인덱스를 검사하지 않으므로 1에서 3 사이로 유지하십시오.
</div>

| 메서드 | 설명 |
| --- | --- |
| `MATRIX3:Identity()  -> matrix3` | 단위 행렬을 반환합니다. |
| `MATRIX3:Create(m11, m12, m13, m21, m22, m23, m31, m32, m33)  -> matrix3` | 행 우선 순서의 아홉 원소로 행렬을 만듭니다. |
| `MATRIX3:YawPitchRoll(yaw, pitch, roll)  -> matrix3` | yaw, pitch, roll 각도로 만든 회전 행렬을 반환합니다. |

### Matrix3 핸들

3 x 3 행렬입니다.

| 메서드 | 설명 |
| --- | --- |
| `matrix3:Get(r, c)  -> number` | r행 c열의 원소를 반환합니다. |
| `matrix3:Set(r, c, v)  -> nil` | r행 c열의 원소를 설정합니다. |
| `matrix3:Mul(o)  -> matrix3` | 행렬 곱을 반환합니다. |
| `matrix3:MulVec(v)  -> vector3` | 3D 벡터와의 곱을 반환합니다. |
| `matrix3:Scale(s)  -> matrix3` | 모든 원소에 s를 곱한 행렬을 반환합니다. |
| `matrix3:Transpose()  -> matrix3` | 전치 행렬을 반환합니다. |
| `matrix3:Determinant()  -> number` | 행렬식을 반환합니다. |
| `matrix3:Clone()  -> matrix3` | 독립된 사본을 반환합니다. |

### MATRIX4

4 x 4 행렬의 팩토리입니다.

<div class="callout warn">
전역 MATRIX4로 사용할 수 있습니다. Get과 Set은 인덱스를 검사하지 않으므로 1에서 4 사이로 유지하십시오. Translation은 오프셋을 네 번째 열에 저장하므로 MulVec를 통해 W = 1인 vector4에 적용됩니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `MATRIX4:Identity()  -> matrix4` | 단위 행렬을 반환합니다. |
| `MATRIX4:Translation(x, y, z)  -> matrix4` | 이동 행렬을 반환합니다. |
| `MATRIX4:Scaling(x, y, z)  -> matrix4` | 스케일 행렬을 반환합니다. |

### Matrix4 핸들

4 x 4 행렬입니다.

| 메서드 | 설명 |
| --- | --- |
| `matrix4:Get(r, c)  -> number` | r행 c열의 원소를 반환합니다. |
| `matrix4:Set(r, c, v)  -> nil` | r행 c열의 원소를 설정합니다. |
| `matrix4:Mul(o)  -> matrix4` | 행렬 곱을 반환합니다. |
| `matrix4:MulVec(v)  -> vector4` | 4D 벡터와의 곱을 반환합니다. |
| `matrix4:Scale(s)  -> matrix4` | 모든 원소에 s를 곱한 행렬을 반환합니다. |
| `matrix4:Transpose()  -> matrix4` | 전치 행렬을 반환합니다. |
| `matrix4:Clone()  -> matrix4` | 독립된 사본을 반환합니다. |

## 쿼터니언

### QUATERNION

쿼터니언의 팩토리입니다.

<div class="callout warn">
전역 QUATERNION으로 사용할 수 있습니다. 단위원은 (0, 0, 0, 1)입니다. 회전 메서드는 단위 쿼터니언을 가정하므로 직접 만든 뒤에는 Normalized를 호출하십시오.
</div>

| 메서드 | 설명 |
| --- | --- |
| `QUATERNION:Identity()  -> quaternion` | 단위 쿼터니언을 반환합니다. |
| `QUATERNION:Create(x, y, z, w)  -> quaternion` | 네 성분으로 쿼터니언을 만듭니다. |
| `QUATERNION:FromAxisAngle(ax, ay, az, angle)  -> quaternion` | 축 (ax, ay, az)를 기준으로 `angle` 라디안 회전하는 쿼터니언을 반환합니다. 이 메서드는 축을 정규화하며, 0 축은 단위원이 됩니다. |

### 쿼터니언 핸들

X, Y, Z, W 필드를 가진 쿼터니언입니다.

| 메서드 | 설명 |
| --- | --- |
| `quaternion.X  -> number` | x 성분(읽고 쓸 수 있는 필드). |
| `quaternion.Y  -> number` | y 성분(읽고 쓸 수 있는 필드). |
| `quaternion.Z  -> number` | z 성분(읽고 쓸 수 있는 필드). |
| `quaternion.W  -> number` | w 성분(읽고 쓸 수 있는 필드). |
| `quaternion:Mul(o)  -> quaternion` | 해밀턴 곱 this * o를 반환합니다. 결과를 적용하면 먼저 o로, 그다음 this로 회전합니다. |
| `quaternion:Dot(o)  -> number` | 내적을 반환합니다. |
| `quaternion:Length()  -> number` | 길이를 반환합니다. |
| `quaternion:Normalized()  -> quaternion` | 단위 길이 사본을 반환하며, 길이가 0에 가까우면 단위원. |
| `quaternion:Conjugate()  -> quaternion` | (-X, -Y, -Z, W), 즉 단위 쿼터니언의 역회전을 반환합니다. |
| `quaternion:RotateVec(vx, vy, vz)  -> number, number, number` | 벡터 (vx, vy, vz)를 회전하고 결과 x, y, z를 반환합니다. |
| `quaternion:Slerp(o, t)  -> quaternion` | 최단 호를 따라 비율 t만큼 o를 향한 구면 선형 보간을 반환합니다. |
| `quaternion:Clone()  -> quaternion` | 독립된 사본을 반환합니다. |
| `quaternion:Unpack()  -> number, number, number, number` | X, Y, Z, W를 네 값으로 반환합니다. |
