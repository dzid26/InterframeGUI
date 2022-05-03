# InterframeGUI




GUI wrapper for [Interframe](https://www.spirton.com/interframe-2-5-0-released/), [AviSynth](http://avisynth.nl/index.php/Main_Page) and [MEncoder](https://www.trishtech.com/2019/07/mencoder-fast-command-line-video-encoding-tool-for-windows/) command line tools for managing and queuening video conversions to higher framerates, eg. 60fps

Use default template:
```# Setmemorymax(900)
cores=4
SetMTMode(3,cores)
<PluginPath>
LoadPlugin(PluginPath+"svpflow1.dll")
LoadPlugin(PluginPath+"svpflow2.dll")
LoadPlugin(PluginPath+"avss.dll")
Import(PluginPath+"InterFrame2.avsi")
<input>.ConvertToYV12()
SetMTMode(2)
InterFrame(GPU=false, Tuning="Film", Preset="Medium", NewNum=60000, NewDen=1001, Cores=cores)
```

For the reference: https://highframerate.wordpress.com/interframegui-download/

### Build 
Create project using Visual Studio 2012 (or higher?). 
