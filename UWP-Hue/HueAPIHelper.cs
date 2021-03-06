﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using UWP_Hue.Models;
using System.Collections.ObjectModel;
using Windows.UI;

namespace UWP_Hue
{
    public static class HueAPIHelper
    {
        public static string api = "http://192.168.168.20/api";
        public static string username = "";

        public async static Task<Boolean> getUsernameCredentials()
        {
            String postContent = "{\"devicetype\":\"hue-uwp#coole-device\"}";
            HttpClient http = new HttpClient();

            var hueStore = HueStore.Instance;

            var result = await http.PostAsync(api, new StringContent(postContent, Encoding.UTF8));
            Debug.Write(result);

            var resultContent = await result.Content.ReadAsStringAsync();

            if (resultContent.Contains("success")) {
                JsonArray jsonArray = JsonArray.Parse(resultContent);
                var firstContent = jsonArray.First().ToString();

                Authentication mappedResult = JsonConvert.DeserializeObject<Authentication>(firstContent);

                hueStore.authenticationObject = mappedResult;
                username = HueStore.Instance.authenticationObject.success.username;
                return true;
            } else
            {
                return false;
            }
            
        }

        private async static Task<string> getLightsFromService()
        {
            HttpClient http = new HttpClient();

            var response = await http.GetAsync(api + "/" + username + "/lights");

            var statusCode = response.StatusCode;

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async static Task parseLights(ObservableCollection<Light> lightsCollection)
        {
            String httpResponse = await getLightsFromService();
            lightsCollection.Clear();
            HueStore.Instance.lights.Clear();

            var lights = HueStore.Instance.lights;

            Light light;

            try
            {
                JsonObject bridgeObject = JsonObject.Parse(httpResponse);
                // var lightObject = bridgeObject.GetNamedObject("lights");

                foreach (string lightID in bridgeObject.Keys)
                {
                    JsonObject lightUniqueObject = bridgeObject.GetNamedObject(lightID);
                    JsonObject stateObject = lightUniqueObject.GetNamedObject("state");

                    light = new Light();
                    light.Id = Convert.ToInt32(lightID);
                    light.Name = lightUniqueObject.GetNamedString("name");
                    light.On = stateObject.GetNamedBoolean("on");
                    light.Hue = Convert.ToInt32(stateObject.GetNamedNumber("hue"));
                    light.Saturation = Convert.ToInt32(stateObject.GetNamedNumber("sat"));
                    light.Brightness = Convert.ToInt32(stateObject.GetNamedNumber("bri"));

                    light.Colour = ColorFromHSV((double)light.Hue, (double)light.Saturation, (double)light.Brightness);

                    lights.Add(light);
                    lightsCollection.Add(light);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Write(e);
            }
        }



        public async static Task putLightChanges(string changedString, int lightId)
        {
            try
            {
                StringContent content = new StringContent(changedString, Encoding.UTF8, "application/json");

                HttpClient http = new HttpClient();
                await http.PutAsync(api + "/" + username + "/lights/" + lightId + "/state", content);
            } catch(OperationCanceledException e)
            {
                Debug.WriteLine(e);
            }
        }

        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, (byte)v, (byte)t, (byte)p);
            else if (hi == 1)
                return Color.FromArgb(255, (byte)q, (byte)v, (byte)p);
            else if (hi == 2)
                return Color.FromArgb(255, (byte)p, (byte)v, (byte)t);
            else if (hi == 3)
                return Color.FromArgb(255, (byte)p, (byte)q, (byte)v);
            else if (hi == 4)
                return Color.FromArgb(255, (byte)t, (byte)p, (byte)v);
            else
                return Color.FromArgb(255, (byte)v, (byte)p, (byte)q);
        }
    }
}
