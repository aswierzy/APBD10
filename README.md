1. appsettings.json template:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "MY_DB" : "put your connection string here"
  }
}
```


2. I've made this in one project and splitted contents into folders inside because the scaffold was made in scope on my project so all the models and context were there and
    since dbcontext handles CRUD the repository and service aren't necessary for such small project, so overall its simpler, more convenient and works better that way.
  
