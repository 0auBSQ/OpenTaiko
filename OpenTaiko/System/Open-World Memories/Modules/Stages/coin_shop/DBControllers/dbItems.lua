local M = {
	DBHook = nil
}

function M:GetItems(slot)
	if M.DBHook == nil then
		M.DBHook = SQL:OpenSQLDatabase("Databases/Items.db3")
	end

	local query = "SELECT * FROM itempool WHERE Slot = '" .. slot .. "'"

	return M.DBHook:Query(query)
end


return M
