# Brokenmai-iOS

A highly WIP project that brings your favourite washing machine experience on you iPad (Only the controls)

## What's the point of all this?
Yeah, it's quite strange to bring only the sensors to iPad. But I find it a good chance to try out Swift and C# at the same time and learn some basic socket
communication. This project is heavily inspired by esterTion's [Brokenithm-iOS](https://github.com/esterTion/Brokenithm-iOS), and thus the name "Brokenmai".

It's my first time in iOS and C# development so the result is pretty ugly, so don't expect a full-fledge laundry simulator on iPad in the foreseeable future.
 However, any help in the development or bug fixing would be appreciated. Just feel free to make a pull request!

## Features
- [x] Touch sensor display  
- [x] Touch control(Kind of)  
- [ ] Virtual button  
- [ ] Reset sensors  
- [ ] Game screen caputure  
- [ ] Many more features I didn't think of

## Known issues
- Two consecutive touches happen on the same sensor will only registered as one touch, the second touch will not be registered
until user touches elsewhere (Hopefully it's not a bug in the game software)
- The button in the corner would not work properly (can't make it clickable for some reasons)
- Obviously unplayable in higher difficulty until streaming game's video output is possible
