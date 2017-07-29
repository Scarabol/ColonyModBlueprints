#!/bin/bash

# exit on error
set -e

mcs /target:library -r:../../../../colonyserver_Data/Managed/Assembly-CSharp.dll -out:Blueprints.dll -sdk:2 src/*.cs

echo '{
	"assemblies" : [
		{
			"path" : "Blueprints.dll",
			"enabled" : true
		}
	]
}' > modInfo.json


