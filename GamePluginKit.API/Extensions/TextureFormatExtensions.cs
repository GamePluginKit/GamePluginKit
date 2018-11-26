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

namespace GamePluginKit.Extensions
{
    public static class TextureFormatExtensions
    {
        public static RenderTextureFormat ToRenderTextureFormat(this TextureFormat self)
        {
            switch (self)
            {
            case TextureFormat.ARGB32:   return RenderTextureFormat.ARGB32;
            case TextureFormat.RGB565:   return RenderTextureFormat.RGB565;
            case TextureFormat.ARGB4444: return RenderTextureFormat.ARGB4444;
            case TextureFormat.RGFloat:  return RenderTextureFormat.RGFloat;
            case TextureFormat.RGHalf:   return RenderTextureFormat.RGHalf;
            case TextureFormat.RFloat:   return RenderTextureFormat.RFloat;
            case TextureFormat.RHalf:    return RenderTextureFormat.RHalf;
            case TextureFormat.R8:       return RenderTextureFormat.R8;
            case TextureFormat.BGRA32:   return RenderTextureFormat.BGRA32;
            case TextureFormat.RG16:     return RenderTextureFormat.RG16;
            default:                     return RenderTextureFormat.Default;
            }
        }
    }
}
