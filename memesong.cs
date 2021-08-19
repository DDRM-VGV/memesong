using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using GlobalEnums;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memesong
{
    
    public class Memesong : Mod
    {

        internal static Memesong Instance;
        static System.Random random = new System.Random();

        public override string GetVersion()
        {
            return "1.0";
        }
        public List<AudioClip> AreaClips = new List<AudioClip>();
        public List<AudioClip> WinClips = new List<AudioClip>();
        public List<AudioClip> LossClips = new List<AudioClip>();
        public List<AudioClip> InterruptClips = new List<AudioClip>();

        public List<AudioSource> players = new List<AudioSource>();

        public void GetClipsFromResources(){
            Assembly asm = Assembly.GetExecutingAssembly();
            foreach (string res in asm.GetManifestResourceNames())
            {   
                if(!res.EndsWith(".wav")) { continue; } 
                using (Stream s = asm.GetManifestResourceStream(res))
                {
                        if (s == null) continue;
                        byte[] buffer = new byte[s.Length];
                        s.Read(buffer, 0, buffer.Length);
                        if(res.Contains("wins")){
                            WinClips.Add(WavUtils.ToAudioClip(buffer));
                        }
                        if(res.Contains("losses")){
                            LossClips.Add(WavUtils.ToAudioClip(buffer));
                        }
                        if(res.Contains("area")){
                            AreaClips.Add(WavUtils.ToAudioClip(buffer));
                        }
                        if(res.Contains("interrupts")){
                            WinClips.Add(WavUtils.ToAudioClip(buffer));
                            LossClips.Add(WavUtils.ToAudioClip(buffer));
                            InterruptClips.Add(WavUtils.ToAudioClip(buffer));
                        }
                        s.Dispose();
                }
            }
        }

        public override void Initialize()
        {
            Instance = this;
            GetClipsFromResources();
            ModHooks.Instance.HeroUpdateHook += update;
            ModHooks.Instance.OnRecieveDeathEventHook += EnemyDied;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneChange;
            ModHooks.Instance.BeforePlayerDeadHook += OnDeath;

            poolPlayers(5);
        }

        public void poolPlayers(int count){
            for(var i =0 ; i < count ; i++){
                var go = new GameObject("audio player ");
                var asrc = go.AddComponent<AudioSource>();
                players.Add(asrc);
                UnityEngine.Object.DontDestroyOnLoad(go);
            }
            Log(" total players : "+ players.Count);
        }
        public AudioSource getPlayerFromPool(){
           AudioSource asrc = null;
           foreach(var p in players){
               if(!p.isPlaying){
                asrc = p;
                break;
               }
           }
           return asrc;
        }
        public void play(AudioClip clip){
           AudioSource asrc = getPlayerFromPool();
           if(asrc == null){
               poolPlayers(5);
                asrc = getPlayerFromPool();
           }
           asrc.PlayOneShot(clip);
        }


        public bool EnemyDied( EnemyDeathEffects enemyDeathEffects, bool eventAlreadyRecieved,ref float? attackDirection, ref bool resetDeathEvent,ref bool spellBurn, ref bool isWatery ){
            if(!eventAlreadyRecieved){
                play(WinClips[random.Next(WinClips.Count)]);
            }
            return true;
        }
        public void SceneChange(Scene scene,LoadSceneMode mode){
            play(AreaClips[random.Next(AreaClips.Count)]);
        }
        public void OnDeath() {
            play(LossClips[random.Next(LossClips.Count)]);
        }

        public int currentTrack = 0;
        public void update()
        {
            // if needed
            if(false && Input.GetKeyDown(KeyCode.C)){
                if(currentTrack == WinClips.Count){
                    currentTrack = 0;
                } else {
                    currentTrack+=1;
                }
                play(WinClips[currentTrack]);
            }
        }

    }

}
