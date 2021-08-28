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
            ModHooks.Instance.TakeHealthHook += TakeDamage;
            poolPlayers(5);
            GameManager.instance.StartCoroutine(playRandomly());
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
        public IEnumerator playRandomly(){
            var chance = 0.05;
            while(true){
                yield return new WaitForSeconds(30);
                if(chance * 100 >= random.Next(100)){
                    //should play
                    play(InterruptClips[random.Next(InterruptClips.Count)]);
                };
            }
        }
        public void play(AudioClip clip){
           AudioSource asrc = getPlayerFromPool();
           if(asrc == null){
               poolPlayers(5);
                asrc = getPlayerFromPool();
           }
           asrc.PlayOneShot(clip,GameManager.instance.GetImplicitCinematicVolume());
        }

        public AudioSource scenePlayer;
        public void playForScene(AudioClip clip,bool forced = false){
            if(scenePlayer == null){
                var go = new GameObject("scene audio player");
                scenePlayer = go.AddComponent<AudioSource>();
                UnityEngine.Object.DontDestroyOnLoad(go);
            }
            if(forced){
                scenePlayer.Stop();
            }
            if(!scenePlayer.isPlaying) {
                scenePlayer.PlayOneShot(clip,GameManager.instance.GetImplicitCinematicVolume());
            }
        }


        public bool EnemyDied( EnemyDeathEffects enemyDeathEffects, bool eventAlreadyRecieved,ref float? attackDirection, ref bool resetDeathEvent,ref bool spellBurn, ref bool isWatery ){
            if(!eventAlreadyRecieved){
                play(WinClips[random.Next(WinClips.Count)]);
            }
            return true;
        }
        public void SceneChange(Scene scene,LoadSceneMode mode){
            var roll = random.Next(100);            
            if(70 >= roll){
                playForScene(AreaClips[random.Next(AreaClips.Count)]);
            }
        }
        public void OnDeath() {
            playForScene(LossClips[random.Next(LossClips.Count)],true);
        }
        public int TakeDamage( int damage ){
            var roll = random.Next(100);            
            if(10 >= roll){
                play(InterruptClips[random.Next(InterruptClips.Count)]);
            } else if(15 >= roll){
                play(LossClips[random.Next(LossClips.Count)]);
            } else if(16 >= roll){
                play(WinClips[random.Next(WinClips.Count)]);
            }
            return damage;
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
