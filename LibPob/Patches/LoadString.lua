--[[
	MoonSharp doesn't have a built in loadstring function.
	Default to load. This should be loadsafe but should work for now.
]]

loadstring = loadstring or load