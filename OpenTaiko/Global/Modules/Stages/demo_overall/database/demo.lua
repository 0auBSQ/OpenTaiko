-- demo_overall/database/demo.lua — a local and a global database: S adds one to the local value,
-- D takes one away, and the global value adds up the local one on every press.

local D = {}

local val1 = 0
local val2 = 0

local localStore = nil
local globalStore = nil

local text = nil
local textTex1 = nil
local textTex2 = nil

local function readInt(store)
	local ret = store:Read("entry")
	if ret == nil then
		return 0
	end
	return tonumber(ret)
end

local function incrementValues(offset)
	globalStore:Write("entry", tostring(val2 + val1))
	localStore:Write("entry", tostring(val1 + offset))

	val1 = readInt(localStore)
	val2 = readInt(globalStore)

	textTex1 = text:GetText(tostring(val1))
	textTex2 = text:GetText(tostring(val2))
end

function D.draw()
	if textTex1 ~= nil then
		textTex1:Draw(200,200)
	end

	if textTex2 ~= nil then
		textTex2:Draw(800,200)
	end
end

function D.update()

	if INPUT:KeyboardPressed("S") then
		-- Increment the counter here
		incrementValues(1)
	end

	if INPUT:KeyboardPressed("D") then
		-- Decrement the counter here
		incrementValues(-1)

	end

	if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
		return Exit("stage", "my_room")
	end

end

function D.activate()
	localStore = DATABASE:OpenLocalDatabase("database/Databases/test.optkdb")
	globalStore = DATABASE:OpenGlobalDatabase("TestSampleDatabases/testGlobal.optkdb")

	val1 = readInt(localStore)
	val2 = readInt(globalStore)

	textTex1 = text:GetText(tostring(val1))
	textTex2 = text:GetText(tostring(val2))
end

function D.deactivate()
	localStore:Dispose()
	globalStore:Dispose()
end

function D.onStart()
	text = TEXT:Create(16)
end

function D.onDestroy()

end

return D
