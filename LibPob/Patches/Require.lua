--[[
	Shim version of lcurl.safe.easy
]]
local easyModule = {}
local local_url, local_callback

function easyModule.setopt_url(self, url)
	local_url = url
end

function easyModule.setopt_writefunction(self, callback)
	local_callback = callback
end

function easyModule.perform()
	local obj = GetMainObject()

	if obj and obj.DownloadPage then
		GetMainObject().DownloadPage(local_url, local_callback, "")
	else
		error("MainObject.DownloadPage is invalid")
	end
end

function easyModule.close()
end

--[[
	Shim version of lcurl.safe
]]

local curl_shim = {}

function curl_shim.easy()
	return easyModule
end


--[[
	Require patch
]]
local l_require = require
function require(name)
	-- Hack to stop it looking for lcurl, which we don't really need
	-- ^ Turns out it's needed. Patch in shim that's pre-loaded
	if name == "lcurl.safe" then
		return curl_shim
	end
	return l_require(name)
end