# RaceDay stand-in API

A small Node.js API that provides every route the RaceDay Part 3 MVC site calls, so the site can be run and demonstrated without the original Part 2 project. It is not the Part 2 submission. Uploaded images are stored in the local uploads folder instead of Azure Blob Storage.

## Run

    npm install
    npm start

The API listens on http://localhost:5080. Check it by opening http://localhost:5080/api/events in a browser.

## Demo accounts

Organiser: organiser@raceday.test with password Organiser123!
Participant: thandi@raceday.test with password Runner1234!

New participants can register through the site. Data is saved in data.json, so delete that file to reset to the sample data.

## Settings (optional environment variables)

PORT (default 5080), PUBLIC_URL (default http://localhost:5080, the address browsers use to load uploaded images), JWT_SECRET.
