# Optional asset regeneration on a Windows PC with the Huihui desktop voice.
# The APK already includes the generated WAV files and never calls system TTS.
Add-Type -AssemblyName System.Speech
Set-Location (Join-Path $PSScriptRoot '..')
$speaker = New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
 $speaker.SelectVoice('Microsoft Huihui Desktop')
 $speaker.Rate=1
 $format=New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(22050,[System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen,[System.Speech.AudioFormat.AudioChannel]::Mono)
 $lines=Get-Content -Encoding UTF8 app/src/main/assets/voice-lines.json -Raw | ConvertFrom-Json
 foreach($line in $lines.PSObject.Properties){
  $output=Join-Path (Get-Location) ('app/src/main/assets/voice/'+$line.Name+'.wav')
  $speaker.SetOutputToWaveFile($output,$format)
  $speaker.Speak([string]$line.Value)
  $speaker.SetOutputToNull()
 }
} finally {$speaker.Dispose()}
