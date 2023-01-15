# Unai.VITC
![VITC example](img/vitc-4px-60s.png)

**Unai.VITC** is a simple VITC (vertical interval timecode) signal generator, written in C# for the .NET Core 3.1 platform.

## Usage
`Unai.VITC` is a console application: you must use it through a terminal and control how it works by passing arguments to it (like changing the framerate; see below).

If no arguments are specified, the program's default behaviour will write the result on the console's standard output as a **raw bitmap** stream, resolution **90x2** and **8-bit grayscale** pixel format.
It will default to a **25 FPS progresive video** and will render indefinitely.
Also, you will get a bunch of gibberish getting printed on your console, which represent the generated VITC code.
Press <kbd>Ctrl</kbd>+<kbd>C</kbd> on the terminal to stop it.

To save the output to a file, you can redirect it:

```Unai.VITC.exe > my-vitc.raw```

Or you can also do the same thing using the `-o` parameter:

```Unai.VITC.exe -o my-vitc.raw```

### Generate a video file
`Unai.VITC` only generates the raw VITC signal. If you want to take the output and turn it into a video file, you can use FFmpeg.
In this case, we can use a simple pipeline to communicate both processes. The command should start like this:

```
Unai.VITC.exe | ffmpeg.exe -f rawvideo -video_size 90x2 -pixel_format gray -framerate 25 -i - […]
```

This will make FFmpeg take the VITC raw signal and interpret it like a raw video stream. Now we can finally generate a VITC signal and save it as a video file:

```
Unai.VITC.exe | ffmpeg.exe -f rawvideo -video_size 90x2 -pixel_format gray -framerate 25 -i - -f mp4 -c:v h264 -pix_fmt yuv420p -vf scale=360:4:flags=neighbor my-vitc.mp4
```

**NOTE**: since there are some video codecs that cannot process video streams below *n* pixels of resolution, you must upscale it.
And to avoid a blurry output, make sure it's using nearest neighbor mode when upscaling.
Also, make sure you are converting the 8-bit grayscale pixel format to another pixel format if the former is not supported by the output codec (in this case, H.264 uses YUV 4:2:0).

### Results
FFmpeg has a video filter called `readvitc` which allows us to decode VITC lines from a video.
All the possible framerates have been tested with this filter, giving the following results:

**24 FPS**

![VITC 24 FPS example](img/readvitc-film.png)

**25 FPS**

![VITC 25 FPS example](img/readvitc-pal.png)

**29.97 FPS**

![VITC 29.97 FPS example](img/readvitc-ntsc-drop.png)

**30 FPS**

![VITC 30 FPS example](img/readvitc-ntsc.png)

## Arguments

### `-i`: input file
It determines the video file/stream that the VITC lines will be stamped on.
This does not overwrite the original file. Use `-o` to specify the output file.
If this option is not specified, no base video is used and instead, only the VITC line is rendered on the output file.

### `-o`: output file
It determines the output file where the result will be saved at.
Use `-` (dash) to indicate the standard output of the console. Default: `-`.

### `-f`: set pixel format
It indicates the pixel format of both input and output video streams. Default: `Grayscale8`.

Common values are `Grayscale8` and `R8G8B8` (24-bit RGB).

### `-s`: set frame size
Sets the input and/or output frame size in pixels. Default: `90x2`.

`-s WxH` where:
- `W` is width.
- `H` is height.

### `-fps`: set framerate
It allows you to set the frames per second, but the only accepted values are 24, 25, and 30 (29.97). Default: `25`.

`-fps [24/25/30]`

#### Example: set framerate to 30 (ATSC)
`-fps 30`

#### Example: set framerate to 29.97 (NTSC)
`-fps 30 -d` (see also the `-d` modifier)

### `-tc`: set initial timecode
It changes the initial timecode. Default: `00:00:00:00`.

`-tc HH:MM:SS:FF`

#### Example: start timecode at 14:59:00, frame 6
`-tc 14:59:00:06`

### `-t`: set duration
It determines how long will be the output. Default: indefinitely.

`-t HH:MM:SS:FF`

#### Example: only render the first 15 seconds
`-t 00:00:15:00`

### `-ev`: set an event
You can set an event at a specified time in order to change parameters or variables.
`EventType` must be `UserBits`, `UserBitsClear`, and `Timecode` (case insensitive).
Some events may require an additional parameter that must be joined with the `EventType` field with a `=` character.

`-ev HH:MM:SS:FF EventType=input`

#### `UserBits` event type
In the VITC signal, there are some reserved bits that can be used to transmit a maximum of 4 custom bytes per frame.
Those custom bits are called UserBits and can be set at any time with the `-ev` modifier.
To clean the user bits, you can use `UserBitsClear`.

#### `UserBitsClear` event type
Clears all the user bits. That is, the user bits are filled with zeros.

#### `Timecode` event type
It allows you to change the timecode at any time.

#### Example: set the user bits to "Test" at the beginning
`-ev 00:00:00:00 UserBits=Test`

#### Example: change the timecode to 23:00:00:00 when reaching 00:01:00:00
`-ev 00:01:00:00 TimeCode=23:00:00:00`

### `-I`: interlaced mode
Specifies whether the video is interlaced or not.
In the case of being interlaced, the program will generate two VITC lines per frame: one for each field.

### `-d`: Drop-frame mode
Specifies whether use drop-frame time code or not.
If 30 FPS video is indicated with this option enabled, then the program will generate a 29.97 FPS VITC instead.

## Example with multiple arguments
Create a VITC signal of 10 seconds long. At second 2, set the user bits to "Test" and at second 4, clear them.

`Unai.VITC.exe -t 00:00:10:00 -ev 00:00:02:00 UserBits=Test -ev 00:00:04:00 UserBitsClear`

### Result
![VITC example 2](img/vitc-4px-10s.png)
