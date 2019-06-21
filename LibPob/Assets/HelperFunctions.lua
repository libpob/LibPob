-- The build module; once a build is loaded, you can find all the good stuff in here
local build = mainObject.main.modes["BUILD"]

-- Here's some helpful helper functions to help you get started
function NewBuild()
	mainObject.main:SetMode("BUILD", false, "Help, I'm stuck in Path of Building!")
	runCallback("OnFrame")
end
function LoadBuildFromXML(xmlText)
	mainObject.main:SetMode("BUILD", false, "", xmlText)
	runCallback("OnFrame")
end
function LoadBuildFromJSON(getItemsJSON, getPassiveSkillsJSON)
	mainObject.main:SetMode("BUILD", false, "")
	runCallback("OnFrame")
	local charData = build.importTab:ImportItemsAndSkills(getItemsJSON)
	build.importTab:ImportPassiveTreeAndJewels(getPassiveSkillsJSON, charData)
	-- You now have a build without a correct main skill selected, or any configuration options set
	-- Good luck!
end