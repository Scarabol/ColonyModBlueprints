# important variables
version = "3.0"
moddir = "Scarabol/Blueprints"
zipname = "$(moddir)/ColonyModBlueprints-$(version)-mods.zip"

dllname = "Blueprints.dll"

#
# actual build targets
#

default:
	mcs /target:library -r:../../../../colonyserver_Data/Managed/Assembly-CSharp.dll -out:"$(dllname)" -sdk:2 src/*.cs
	echo '{\n\t"assemblies" : [\n\t\t{\n\t\t\t"path" : "$(dllname)",\n\t\t\t"enabled" : true\n\t\t}\n\t]\n}' > modInfo.json

clean:
	rm -f "$(dllname)" "modInfo.json"

all: clean default

release: default
	rm -f "$(zipname)"
	cd ../../ && zip "$(zipname)" "$(moddir)/modInfo.json" "$(moddir)/$(dllname)" "$(moddir)/blueprints/*"

