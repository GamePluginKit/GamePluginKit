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

using UnityEngine;

namespace GamePluginKit.API.Extensions
{
    public static class Texture2DExtensions
    {
        public static Texture2D ToReadable(this Texture2D self)
        {
            var prevRender = RenderTexture.active;
            var tempRender = RenderTexture.GetTemporary(
                width:       self.width,
                height:      self.height,
                depthBuffer: 0,
                format:      self.format.ToRenderTextureFormat(),
                readWrite:   RenderTextureReadWrite.Linear
            );

            Graphics.Blit(self, tempRender);
            RenderTexture.active = tempRender;

            var texture = new Texture2D(self.width, self.height);
            texture.ReadPixels(new Rect(0, 0, self.width, self.height), 0, 0);
            texture.Apply();

            RenderTexture.active = prevRender;
            RenderTexture.ReleaseTemporary(tempRender);

            return texture;
        }
    }
}
