# BingBac2
A Bing background wallpaper downloader

Downloads the Bing image of the day to the BingBac2 folder and sets your desktop wallpaper.

Based on a similar project called BingBackground, no code has been reused. This has been completely rewritten in dotnet core as a personal project. The original application retrieved lower resolution images for smaller screens. I am not going to fuss with that because the max available from the Bing endpoint right now is 1920x1080 and that is about the minimum on most devices now.

- TODO: Refactor Program.cs and move more functionality into the BingBacService
- TODO: Create the wallpapers directory if it doesn't exist
- TODO: Test edge cases
- TODO: I don't know too much about dotnet core releases yet, but we probably want some sort of publish step to create a version of the app that
can easily be copied to a folder and executed.