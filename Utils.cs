using System;
using System.IO;
using System.Reflection;
using UnityEngine;

using static Modding.Logger;
namespace Memesong
{
    public static class Utils {

        public static System.Random random = new System.Random();
        public static string path = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),"Memesong");
        public static string wins = "wins";
        public static string losses = "losses";
        public static string area = "area";
        public static string interrupts = "interrupts";
        
        public static void ExtractAudioFiles(){
            Debug.Log("Attempt to extract");
            if(!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            } else {
                return;
                Log("Memesong Directory Already Exists: Skipping extraction");
            }
            Assembly asm = Assembly.GetExecutingAssembly();
            string finalPath = path;
            foreach (string res in asm.GetManifestResourceNames())
            {   
                if(!res.EndsWith(".wav")) {
                    continue;
                } 

                if(res.Contains(wins)){
                    finalPath = Path.Combine(path,wins);
                } else if(res.Contains(losses)){
                    finalPath = Path.Combine(path,losses);
                } else if(res.Contains(area)){
                    finalPath = Path.Combine(path,area);
                } else if(res.Contains(interrupts)){
                    finalPath = Path.Combine(path,interrupts);
                } else {
                    finalPath = path;
                }

                using (Stream s = asm.GetManifestResourceStream(res))
                {
                        Log(res);
                        if (s == null) continue;
                        var buffer = new byte[s.Length];
                        s.Read(buffer, 0, buffer.Length);
                        if(!Directory.Exists(finalPath)) {
                            Directory.CreateDirectory(finalPath);
                        }
                        var resString = res.Split('.');
                        var fileName = res;
                        if (resString.Length > 2){
                            fileName = resString[(resString.Length - 2)] + "." + resString[(resString.Length - 1)] ;
                        }
                        File.WriteAllBytes(Path.Combine(finalPath,fileName),buffer);
                        s.Dispose();
                }
            }
        }
        

    }
}