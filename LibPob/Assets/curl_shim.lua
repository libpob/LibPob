local easyModule = {}
local local_url, local_callback

function easyModule.setopt_url(self, url)
	local_url = url
end

function easyModule.setopt_writefunction(self, callback)
	local_callback = callback
end

function easyModule.perform()
	GetMainObject().DownloadPage(local_url, local_callback, "")
end

function easyModule.close()
end


local curl = {}

function curl.easy()
	return easyModule
end

return curl