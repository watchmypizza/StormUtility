.PHONY: all build backup sync git restart

all:
	dotnet build
	ssh penut "cp -a ~/stormtility/StormUtility/ ~/stormtility/StormUtilityBackup-$$(date +%Y%m%d-%H%M%S)"
	rsync -avhc --progress bin/Debug/net10.0/linux-arm64/ penut:~/stormtility/StormUtility/
	ssh penut "sudo systemctl restart stormtility"
	git add -A
	git commit
	git push

build:
	dotnet build

backup:
	ssh penut "cp -a ~/stormtility/StormUtility/ ~/stormtility/StormUtilityBackup-$$(date +%Y%m%d-%H%M%S)"

sync:
	rsync -avhc --progress bin/Debug/net10.0/linux-arm64/ penut:~/stormtility/StormUtility/

git:
	git add -A
	git commit
	git push

restart:
	ssh penut "sudo systemctl restart stormtility"
