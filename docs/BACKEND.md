# API Layer --> Application --> Domain, Infra --> Application

Inkboard.Domain: Center of the application, no external deps, 
Ex: Repository interfaces, entities, no implementations, just interfaces. 

Inkboard.Application: Defines what your actual app can do (use cases), controls
the flow of data, contains business logic but abstracts where/how data is stored 
or where requests come from

Inkboard.Infrastructure: Impleemnts the interfaces designed in the application layer.
Databases, email, file storage, third-party APIs.
Ex: DbContext, Entity Framework Core implementations, external services like SendEmailToUser()

Inkboard.API: Receives API requests, hands them off to application layer, and returns
responses, no business logic. Entry point. 