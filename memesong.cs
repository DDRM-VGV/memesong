using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memesong
{
    
    public class Memesong : Mod, ITogglableMod
    {
        internal static Memesong Instance;
        static System.Random random = new System.Random();

        public override string GetVersion()
        {
            return "1.4.2";
        }
        public List<AudioClip> AreaClips = new List<AudioClip>();
        public List<AudioClip> WinClips = new List<AudioClip>();
        public List<AudioClip> LossClips = new List<AudioClip>();
        public List<AudioClip> InterruptClips = new List<AudioClip>();

        public List<AudioSource> players = new List<AudioSource>();

        public int currentTrack = 0;
        private bool Unloaded;
        public AudioClip clips { get; private set; }

        public void GetClipsFromResources(){
            Assembly asm = Assembly.GetExecutingAssembly();
            foreach (string res in asm.GetManifestResourceNames())
            {   
                if(!res.EndsWith(".wav")) continue;
                using (Stream s = asm.GetManifestResourceStream(res))
                {
                        if (s == null) continue;
                        byte[] buffer = new byte[s.Length];
                        s.Read(buffer, 0, buffer.Length);
                        if(res.Contains("wins")){
                            clips = WavUtils.ToAudioClip(buffer);
                            WinClips.Add(clips);
                        }
                        if(res.Contains("losses")){
                            clips = WavUtils.ToAudioClip(buffer);
                            LossClips.Add(clips);
                        }
                        if(res.Contains("area")){
                            clips = WavUtils.ToAudioClip(buffer);
                            AreaClips.Add(clips);
                        }
                        if(res.Contains("interrupts")){
                            clips = WavUtils.ToAudioClip(buffer);
                            WinClips.Add(clips);
                            LossClips.Add(clips);
                            InterruptClips.Add(clips);
                        }
                        s.Dispose();
                }
            }
        }

        public override void Initialize()
        {
            Debug.Log("Initializing Memesong");
            Instance = this;

            AreaClips = new List<AudioClip>();
            WinClips = new List<AudioClip>();
            LossClips = new List<AudioClip>();
            InterruptClips = new List<AudioClip>();
            GetClipsFromResources();

            ModHooks.Instance.HeroUpdateHook += update;
            ModHooks.Instance.OnRecieveDeathEventHook += EnemyDied;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneChange;
            ModHooks.Instance.BeforePlayerDeadHook += OnDeath;
            ModHooks.Instance.TakeHealthHook += TakeDamage;

            if (players.Count < 5)
            {
                poolPlayers(5);
            }
            GameManager.instance.StartCoroutine(playRandomly());

            Unloaded = false;
            Debug.Log("Done initializing Memesong");
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
                if(chance * 100 >= random.Next(100) && !Unloaded){
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
                if (Unloaded) return;
                scenePlayer.PlayOneShot(clip,GameManager.instance.GetImplicitCinematicVolume());
            }
        }


        public bool EnemyDied( EnemyDeathEffects enemyDeathEffects, bool eventAlreadyRecieved,ref float? attackDirection, ref bool resetDeathEvent,ref bool spellBurn, ref bool isWatery ){
            if(!eventAlreadyRecieved){
                if (!Unloaded){
                    play(WinClips[random.Next(WinClips.Count)]);
                }
            }
            return true;
        }
        public void SceneChange(Scene scene,LoadSceneMode mode){
            if (Unloaded) return;
            playForScene(AreaClips[random.Next(AreaClips.Count)]);
        }
        public void OnDeath() {
            if (Unloaded) return;
            playForScene(LossClips[random.Next(LossClips.Count)],true);
        }
        public int TakeDamage( int damage ){
            var roll = random.Next(100);
            if (Unloaded) return damage;
            if (10 >= roll){
                play(InterruptClips[random.Next(InterruptClips.Count)]);
            } else if(15 >= roll){
                play(LossClips[random.Next(LossClips.Count)]);
            } else if(16 >= roll){
                play(WinClips[random.Next(WinClips.Count)]);
            }
            return damage;
        }

        public void update()
        {
            // if needed
            if(false && Input.GetKeyDown(KeyCode.C) && !Unloaded){
                if(currentTrack == WinClips.Count){
                    currentTrack = 0;
                } else {
                    currentTrack+=1;
                }
                play(WinClips[currentTrack]);
            }
        }

        public void Unload()
        {
            Debug.Log("Unloading Memesong");

            GameManager.instance.StopCoroutine(playRandomly());
            foreach (AudioClip clip in WinClips){
                UnityEngine.Object.Destroy(clip);
            }
            foreach (AudioClip clip in AreaClips){
                UnityEngine.Object.Destroy(clip);
            }
            foreach (AudioClip clip in LossClips){
                UnityEngine.Object.Destroy(clip);
            }
            foreach (AudioClip clip in InterruptClips){
                UnityEngine.Object.Destroy(clip);
            }

            AreaClips.Clear();
            InterruptClips.Clear();
            LossClips.Clear();
            WinClips.Clear();

            foreach (var player in players){
                UnityEngine.Object.Destroy(player.gameObject);
            }
            players.Clear();

            ModHooks.Instance.HeroUpdateHook -= update;
            ModHooks.Instance.OnRecieveDeathEventHook -= EnemyDied;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= SceneChange;
            ModHooks.Instance.BeforePlayerDeadHook -= OnDeath;
            ModHooks.Instance.TakeHealthHook -= TakeDamage;

            Instance = null;
            Unloaded = true;
            Debug.Log("Done unloading Memesong");
        }
    }
}
