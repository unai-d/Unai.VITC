# Unai.VITC
![VITC example](img/vitc-4px-60s.png)

**Unai.VITC** is a simple VITC (vertical interval timecode) signal generator and it is written in C# for the .NET Core 3.1 platform.

## Usage
Unai.VITC is a console application: it will read the arguments whether or not specified and it will write the result on the console's standard output as a **raw bitmap** stream, resolution **90x1** and **8-bit grayscale** pixel format.

If you just type this:

```Unai.VITC.exe```

It will default to a 25 FPS progresive video, and render 2500 frames from 00:00:00.00 to 00:01:39.24 (0 to 2499).
Also, you will get a bunch of `ÿ` and ` ` (null) characters on your console.
To save the raw output to a file, you can redirect it:

```Unai.VITC.exe > my-vitc.raw```

### Generate a video
Unai.VITC only generates the raw VITC signal. If you want to take the output and turn it into a video file, you can use FFmpeg.
In this case, we can use a simple pipeline to communicate both processes. The command should start like this:

```
Unai.VITC.exe | ffmpeg.exe -f rawvideo -video_size 90x1 -pixel_format gray -i - [...]
```

This will make FFmpeg take the VITC raw signal and interpret it like a raw video stream. Now we can finally generate a VITC signal and save it as a video file:

```
Unai.VITC.exe | ffmpeg.exe -f rawvideo -video_size 90x1 -pixel_format gray -i - -f mp4 -c:v h264 -pix_fmt yuv420p -vf scale=360:4:flags=neighbor my-vitc.mp4
```

**NOTE**: since there are some video codecs that cannot process video streams below *n* pixels of resolution, you must upscale it.
And to avoid a blurry output, make sure it's using nearest neighbor mode.
Also, make sure you are converting the 8-bit grayscale pixel format to another pixel format supported by the output codec (in this case, YUV 4:2:0).

## Arguments

### `-fps`: set framerate
It allows you to set the frames per second, but the only accepted values are 24, 25, and 30 (29.97). Default: `25`.

`-fps [24/25/30]`

#### Example: set framerate to 29.97 (NTSC)
`-fps 30`
**NOTE**: frame drop is not supported yet, so this will generate a pure 30 FPS video, not 29.97.

### `-tc`: set initial timecode
It changes the initial timecode. Default: `00:00:00:00`.

`-tc HH:MM:SS:FF`

#### Example: start timecode at 14:59:00, frame 6
`-tc 14:59:00:06`

### `-t`: set duration
It determines how long will be the output. Default: `00:01:40:00`.

`-t HH:MM:SS:FF`

#### Example: only render the first 15 seconds
`-t 00:00:15:00`

### `-ev`: set an event
You can set an event at a specified time in order to change parameters or variables.
`EventType` must be `UserBits`, `UserBitsClear`, and `Timecode` (case sensitive).
If the second parameter, `input`, is not used, you must specify `""` anyways.

`-ev HH:MM:SS:FF EventType "input"`

#### `UserBits` event type
In the VITC signal, there are some reserved bits that can be used to transmit a maximum of 4 custom bytes per frame.
Those custom bits are called UserBits and can be set at any time with the `-ev` modifier.
To clean the user bits, you can use `UserBitsClear`.

#### `UserBitsClear` event type
Clears all the user bits. That is, the user bits are filled with zeros.

#### `Timecode` event type
It allows you to change the timecode at any time.

#### Example: set the user bits to "Test" at the beginning
`-ev 00:00:00:00 UserBits "Test"`

#### Example: change the timecode to 23:00:00:00 when reaching 00:01:00:00
`-ev 00:01:00:00 TimeCode "23:00:00:00"`

## Example with multiple arguments
Create a VITC signal of 10 seconds long. At second 2, set the user bits to "Test" and at second 4, clear them.

`Unai.VITC.exe -t 00:00:10:00 -ev 00:00:02:00 UserBits "Test" -ev 00:00:04:00 UserBitsClear ""`

### Result
![VITC example 2](img/vitc-4px-10s.png)
