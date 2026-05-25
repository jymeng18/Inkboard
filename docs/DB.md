# Initial commit for our DB schema

```bash
cd Inkboard.Infra/
dotnet ef migrations add InitialCreate -s ../Inkboard.API
```
-s tells EF that Inkboard.API is the entry point where you can grab the 
connection string and compile the project. 

-InitialCreate is the commit mesasge


# Run this after any changes are made to the schema

```bash
cd Inkboard.Infra/
dotnet ef database update -s ../Inkboard.API
```

