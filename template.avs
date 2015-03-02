# Setmemorymax(900)
SetMTMode(3,6)
<PluginPath>
LoadPlugin(PluginPath+"mvtools2.dll")
LoadPlugin(PluginPath+"RemoveGrainSSE3.dll")
LoadPlugin(PluginPath+"avss.dll")
Import(PluginPath+"InterFrame.avsi")
<input>.ConvertToYV12()
SetMTMode(2)
<deinterlace>
<crop>
<denoise>
<resize>
InterFrame(GPU=true, Tuning="Film", Preset="Medium", NewNum=60000, NewDen=1001, FlowPath=PluginPath)
