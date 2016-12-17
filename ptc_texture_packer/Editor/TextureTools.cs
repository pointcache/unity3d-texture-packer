/* copyright pointcache (antonov.3d@gmail.com) 2016, subject to standard MIT license, 
 * do whatever except reselling, and always keep this notice */

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;

public static class TextureTools
{

    const string TEXTOOLS_MENU = "TexTools/";
    const string SUBSTANCE_MENU = "Substance/";
    const string metallic_suffix = "_metallic";
    const string roughness_suffix = "_roughness";
    const string ao_suffix = "_ao";
    const string emissive_suffix = "_emissive";
    const string unity_roughness_suffix = "_unity_roughness";


    /// <summary>
    /// Select metallic or roughness outputed from substance and it will create new "unityRoughness" texture
    /// </summary>
    [MenuItem(TEXTOOLS_MENU + SUBSTANCE_MENU + "Make unity roughness")]
    public static void CreateUnityRoughness()
    {
        string error1 = "Select _roughness or _metallic";
        string error_roughness = "Roughness texture source not found";
        string error_metallic = "Metallic texture source not found";

        string texture_format = "";

        var selected = Selection.activeObject as Texture2D;
        if (!selected)
        {
            Debug.LogError(error1);
            return;
        }

        if (!selected.name.Contains(metallic_suffix) && !selected.name.Contains(roughness_suffix))
        {
            Debug.LogError(error1);
            return;
        }

        texture_format = Path.GetExtension(AssetDatabase.GetAssetPath(selected));
        string name_without_prefix = selected.name.Replace(metallic_suffix, "").Replace(roughness_suffix, "");
        Texture2D roughness, metallic, ao, emissive;

        string path = Directory.GetParent(AssetDatabase.GetAssetPath(selected)).ToString() + "/";

        roughness = AssetDatabase.LoadAssetAtPath(path + name_without_prefix + roughness_suffix + texture_format, typeof(Texture2D)) as Texture2D;
        if (!roughness)
        {
            Debug.LogError(error_roughness);
            return;
        }

        metallic = AssetDatabase.LoadAssetAtPath(path + name_without_prefix + metallic_suffix + texture_format, typeof(Texture2D)) as Texture2D;
        if (!metallic)
        {
            Debug.LogError(error_metallic);
            return;
        }

        ao = AssetDatabase.LoadAssetAtPath(path + name_without_prefix + ao_suffix + texture_format, typeof(Texture2D)) as Texture2D;
        bool aoFound = ao;
        if (!aoFound)
            Debug.Log("ambient occlusion was not found, check if its the same file format as the selected file");

        emissive = AssetDatabase.LoadAssetAtPath(path + name_without_prefix + emissive_suffix + texture_format, typeof(Texture2D)) as Texture2D;
        bool emissiveFound = emissive;
        if (!emissiveFound)
            Debug.Log("emissive was not found, check if its the same file format as the selected file");

        SetTextureImporterFormat(roughness, true);
        Color32[] roughness_pixels = roughness.GetPixels32();

        SetTextureImporterFormat(metallic, true);
        Color32[] metallic_pixels = metallic.GetPixels32();
        Color32[] ao_pixels = metallic_pixels;
        Color32[] emissive_pixels = new Color32[metallic_pixels.Length];
        if (aoFound)
        {
            SetTextureImporterFormat(ao, true);
            ao_pixels = ao.GetPixels32();
        }

        if (emissiveFound)
        {
            SetTextureImporterFormat(emissive, true);
            emissive_pixels = emissive.GetPixels32();
        }

        Texture2D finaltex = new Texture2D(selected.width, selected.height, TextureFormat.ARGB32, true);
        Color32[] final = new Color32[roughness_pixels.Length];

        int count = roughness_pixels.Length;

        for (int i = 0; i < count; i++)
        {
            Color32 rp = roughness_pixels[i];
            Color32 mp = metallic_pixels[i];
            Color32 aop = ao_pixels[i];
            Color32 ep = emissive_pixels[i];
            final[i] = new Color32(mp.r, aop.g, ep.b, Convert.ToByte(255 - rp.r));
        }

        finaltex.SetPixels32(final);
        finaltex.Apply();

        byte[] bytes = finaltex.EncodeToTGA();
        File.WriteAllBytes(path + name_without_prefix + unity_roughness_suffix + ".tga", bytes);

        SetTextureImporterFormat(roughness, false);
        SetTextureImporterFormat(metallic, false);
        SetTextureImporterFormat(ao, false);
        SetTextureImporterFormat(emissive, false);
        AssetDatabase.Refresh();
    }

    public static void SetTextureImporterFormat(Texture2D texture, bool isReadable)
    {
        if (null == texture) return;

        string assetPath = AssetDatabase.GetAssetPath(texture);
        var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (tImporter != null)
        {
            tImporter.textureType = TextureImporterType.Advanced;

            tImporter.isReadable = isReadable;

            AssetDatabase.ImportAsset(assetPath);
            AssetDatabase.Refresh();
        }
    }
}
