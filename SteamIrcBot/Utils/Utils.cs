﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;
using System.Net;

namespace SteamIrcBot
{
    static class ExtensionUtils
    {
        public static T GetAttribute<T>( this Type type, bool inherit )
            where T : Attribute
        {
            T[] attribs = type.GetCustomAttributes( typeof( T ), inherit ) as T[];

            if ( attribs == null || attribs.Length == 0 )
                return null;

            return attribs[ 0 ];
        }

        public static bool Implements( this Type type, Type interfaceType )
        {
            return type.GetInterfaces()
                .Any( i => i == interfaceType );
        }
    }

    static class SteamUtils
    {
        public static bool TryDeduceSteamID( string input, out SteamID steamId )
        {
            steamId = new SteamID();

            if ( string.IsNullOrEmpty( input ) )
                return false;

            if ( input.StartsWith( "STEAM_", StringComparison.OrdinalIgnoreCase ) )
            {
                steamId = new SteamID( input, EUniverse.Public );
                return true;
            }

            ulong uSteamID;
            if ( ulong.TryParse( input, out uSteamID ) )
            {
                steamId = uSteamID;
                return true;
            }

            if ( ResolveVanityURL( input, out steamId ) )
                return true;

            return false;
        }

        public static bool ResolveVanityURL( string customUrl, out SteamID steamId )
        {
            steamId = new SteamID();

            if ( string.IsNullOrWhiteSpace( customUrl ) )
                return false;

            var apiKey = Settings.Current.WebAPIKey;

            if ( apiKey == null )
            {
                Log.WriteWarn( "SteamUtils", "Unable to use ResolveVanityURL: no web api key in settings" );
                return false;
            }

            using ( dynamic iface = WebAPI.GetInterface( "ISteamUser", apiKey ) )
            {
                KeyValue results = null;

                try
                {
                    results = iface.ResolveVanityURL( vanityurl: customUrl );
                }
                catch ( WebException )
                {
                    return false;
                }

                EResult eResult = ( EResult )results[ "success" ].AsInteger();

                if ( eResult == EResult.OK )
                {
                    steamId = ( ulong )results[ "steamid" ].AsLong();
                    return true;
                }
            }

            return false;
        }

        public static string ExpandSteamID( SteamID input )
        {
            string displayInstance = input.AccountInstance.ToString();

            switch ( input.AccountInstance )
            {
                case SteamID.AllInstances:
                    displayInstance = "all (0)";
                    break;

                case SteamID.DesktopInstance:
                    displayInstance = "desktop (1)";
                    break;

                case SteamID.ConsoleInstance:
                    displayInstance = "console (2)";
                    break;

                case SteamID.WebInstance:
                    displayInstance = "web (4)";
                    break;

                case ( uint )SteamID.ChatInstanceFlags.Clan:
                    displayInstance = "clan (524288 / 0x80000)";
                    break;

                case ( uint )SteamID.ChatInstanceFlags.Lobby:
                    displayInstance = "lobby (262144 / 0x40000)";
                    break;

                case ( uint )SteamID.ChatInstanceFlags.MMSLobby:
                    displayInstance = "mms lobby (131072 / 0x20000)";
                    break;
            }

            return string.Format( "{0} (UInt64 = {1}, IsValid = {2}, Universe = {3}, Instance = {4}, Type = {5}, AccountID = {6})",
                input.ToString(), input.ConvertToUInt64(), input.IsValid, input.AccountUniverse, displayInstance, input.AccountType, input.AccountID );
        }
    }

}
