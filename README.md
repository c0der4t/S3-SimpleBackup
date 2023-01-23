#  S3-SimpleBackup
### ⤴ A simple S3 storage backup app

## Warning, this app is still under development
Although the application can now fully sync files up to an S3 bucket, it is important to understand that the app is still in development and being tested.

## 🛑 Security Warning
The app currently does not encrypt API keys at rest in the s3.ini file. Please opt for typing your keys into the UI manually each time you run the app. 
WE ARE WORKING ON A SECURE WAY TO STORE YOUR KEYS

## 👀 Most release / commit

 Features added:

- Finished indexing method to index directory before syncing
- Added actual syncing function. You can now sync files from a source to S3 successfully
- Added job management and loading system
- Added ability to change buckets for each job. No longer one bucket for all jobs
- Recursive sync is now optional
- Added ability to empty an entire bucket, with warning systems of course
- Converted working methods to asynchronous so UI no longer locks up
- UI output / log not updates live and does not freeze
- Added memory management to UI output to avoid infinite memory usage as the log grows
- Cleaned up settings screen
- Added AppConfig Models
- App Config can now hide the developer tab
- App config now dynamically points to a profile location

Known Bugs:
- Cannot set up S3.ini from UI. Have to do it manually in %appdir%/s3.ini
- Cannot set up settings.ini from UI. Have to do it manually in %appdir%/settings.ini
- API keys are not encrypted at rest. Please type into interface each time for safety and avoid using s3.ini

# 🏞 Join the journey 

If you like what you see, jump on board and stay in touch. Here are the main ways to stay up to date:

- [Join me live on Youtube](https://www.youtube.com/@ekronds)
- Follow the project on [GitHub](https://github.com/c0der4t/EDS_Retail/)
- [Follow our blog](https://blog.ekronds.co.za/series/eds-retail), we have a series dedicated to EDS Retail (Ensure you subscribe to the newsletter)
- [Follow me on Twitter](https://twitter.com/EkronMonte) for the bite sized info
- [Follow me on Polywork](https://www.polywork.com/c0der4t)
