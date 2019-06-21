--[[
	Fix file paths as well as stop first run & auto updating
]]

local l_open = io.open
io.open = function(fileName, ...)
	-- first.run requires a whole bunch of updating/setup
	-- manifest errors out when parsing xml due to string:gsub
	if fileName == "first.run" or fileName == "manifest.xml" then
		return nil
	end

	if fileName ~= nil and not fileName:match("PathOfBuilding") and InstallDirectory then
		fileName = InstallDirectory:gsub("\\", "/") .. "/" .. fileName
	end

	return l_open(fileName, ...)
end