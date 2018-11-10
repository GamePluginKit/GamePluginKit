// Copyright 2018 Benjamin Moir
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using NAudio.Wave;
using UnityEngine;

namespace GamePluginKit.API.Extensions
{
    public static class WaveFileReaderExtensions
    {
        public static AudioClip ToAudioClip(this WaveFileReader input)
            => ToAudioClip(input, false);

        public static AudioClip ToAudioClip(this WaveFileReader input, bool stream)
        {
            var samples = input.ToSampleProvider();
            var fmt     = input.WaveFormat;

            var clip = AudioClip.Create(
                name:              string.Empty,
                lengthSamples:     (int)input.SampleCount,
                channels:          fmt.Channels,
                frequency:         fmt.SampleRate,
                stream:            stream,
                pcmreadercallback: data => samples.Read(data, 0, data.Length)
            );

            return clip;
        }
    }
}
