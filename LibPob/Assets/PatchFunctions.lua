local l_require = require
function require(name)
	-- Hack to stop it looking for lcurl, which we don't really need
	-- ^ Turns out it's needed. Patch in shim that's pre-loaded
	if name == "lcurl.safe" then
		return curl_shim
	end
	return l_require(name)
end

-- Fix file paths
-- TODO: Fix this ugly hack
local l_open = io.open
io.open = function(fileName, ...)
	-- first.run requires a whole bunch of updating/setup
	-- manifest errors out when parsing xml due to string:gsub
	if fileName == "first.run" or fileName == "manifest.xml" then
		return nil
	end

	if fileName ~= nil and not fileName:match("PathOfBuilding") then
		fileName = InstallDirectory .. "\\" .. fileName
	end

	return l_open(fileName, ...)
end