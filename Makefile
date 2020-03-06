build: 
	cd src; \
	dotnet build	


run:
	cd src; \
	dotnet run -- $(PORT)

restore: 
	cd src; \
	dotnet restore	

clean: 
	cd src; \
	dotnet clean	
	

