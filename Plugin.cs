using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Networking;

namespace MusicalShotgun;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    internal static Harmony _Harmony;

    internal static ConfigEntry<string> ConfigSoundfile;
    internal static AudioClip Audio;

    internal static ConfigEntry<float> ConfigAudioStartThreshold;
    internal static ConfigEntry<bool> ConfigShotgunOnly;
    internal static ConfigEntry<bool> ConfigAdditionalDebugLogging;

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} loaded.");
        
        ConfigSoundfile = Config.Bind<string>("Configuration", "SoundFilename", "audio.mp3", "ONLY .MP3 FILES ARE SUPPORTED! Audio file name to use. Place in the same directory as the mod .dll");
        ConfigAudioStartThreshold = Config.Bind<float>("Configuration", "AudioStartThreshold", 0.00f, "How far away the weapon needs to turned (between 0 = facing foward and 1 = facing player) for the audio to start playing");
        ConfigShotgunOnly = Config.Bind<bool>("Configuration", "ShotgunOnly", true, "If true, will only apply to the shotgun. If false, will apply to all guns, including the tranquilizer and pulse pistol, among others.");
        ConfigAdditionalDebugLogging = Config.Bind<bool>("Debug", "AdditionalDebugLogging", false, "Enable extra debug logging. This floods the console with messages, and is most likely unnecessary if you aren't a developer.");

        StartCoroutine(LoadSound());

        _Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _Harmony.PatchAll();
    }

    public IEnumerator LoadSound()
    {
        // This is the only way I could figure out importing sound without having to bundle it into an AssetBundle. With a webrequest. I love Unity.
        Uri uri = new Uri($"{Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ConfigSoundfile.Value)}");
        Logger.LogInfo($"Trying to load audio file at {uri}");
        
        UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG);
        request.timeout = 1;

        yield return request.SendWebRequest();

        Logger.LogInfo($"Request finished with {request.result} (code {request.responseCode})");

        if (request.result == UnityWebRequest.Result.Success)
        {
            Audio = DownloadHandlerAudioClip.GetContent(request);
        }
        else
        {
            Logger.LogError($"Request for URI {uri} returned {request.result} (code {request.responseCode})");
        }
    }
}

/*

    ItemGunPatch
    Patches guns when they enter the world. Doesn't do much more than attach an ItemGunMusicComponent

*/
[Harmony]
public class ItemGunPatch
{
    [HarmonyPatch(typeof(ItemGun), "Start")]
    [HarmonyPostfix]
    private static void StartPostfix(ItemGun __instance)
    {
        if (Plugin.ConfigShotgunOnly.Value && !__instance.name.Contains("Item Gun Shotgun"))
        {
            return;
        }

        Plugin.Logger.LogDebug(__instance);
        __instance.gameObject.AddComponent<ItemGunMusicComponent>();
    }
}

/*

    ItemGunMusicComponent
    This class gets attached to the gun items, and gets run like a normal Unity component.

*/
public class ItemGunMusicComponent : MonoBehaviour
{
    private PhysGrabObject grabObject;
    private AudioSource audioPlayer;
    private bool isAudioPlaying;

    private void Start()
    {
        grabObject = gameObject.GetComponent<PhysGrabObject>();
        
        if (!grabObject)
        {
            Plugin.Logger.LogWarning("Fuh ohCK!!!! THERENS NO DAMN GRABOBJEXCT ON THIS THANG??? ABORT! ABORT!!!");
            
            // Disable self
            gameObject.GetComponent<ItemGunMusicComponent>().enabled = false;
            return;
        }

        if (!Plugin.Audio)
        {
            Plugin.Logger.LogWarning("No audio loaded? Not fun k cc");

            // Disable self
            gameObject.GetComponent<ItemGunMusicComponent>().enabled = false;
            return;
        }

        audioPlayer = gameObject.AddComponent<AudioSource>();
        audioPlayer.clip = Plugin.Audio;
        audioPlayer.loop = true;
    }

    private void Update()
    {
        if (grabObject.grabbedLocal) // I'm not sure why I used PhysGrabObject.grabbedLocal and not just PhysGrabObject.grabbed. I am too scared to change it now.
        {
            float diff = (grabObject.playerGrabbing.Last().transform.rotation.eulerAngles.Abs().y - transform.rotation.eulerAngles.Abs().y); // We only count Y rotation. This couldn't possibly go wrong.
            float vol = (1 - (Math.Abs((Math.Abs(diff) - 180f) / 180f))); // i don't even know man // Converts diff to a range between 0 and 1.
            
            if (Plugin.ConfigAdditionalDebugLogging.Value)
            {
                Plugin.Logger.LogDebug($"DIFF: {diff}, VOL: {vol}, PLAYER: {grabObject.playerGrabbing.Last().transform.rotation.eulerAngles.Abs().y}, PLAYING {isAudioPlaying}");
            }

            if (!isAudioPlaying && vol > Plugin.ConfigAudioStartThreshold.Value) // Audio not playing, threshold met. Play audio.
            {
                audioPlayer.Play();
                isAudioPlaying = true;
            }
            else if (isAudioPlaying && vol < Plugin.ConfigAudioStartThreshold.Value) // Audio playing, threshold not met. Stop audio.
            {
                audioPlayer.Stop();
                isAudioPlaying = false;
            }

            audioPlayer.volume = vol; 
        }
        else if (isAudioPlaying) // Audio playing, but not grabbed. Stop audio.
        {
            isAudioPlaying = false;
            audioPlayer.Stop();
        }
    }
}