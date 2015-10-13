﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Dapplo.Config.Ini;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Native;
using GreenshotPlugin.Configuration;
using log4net;
using System;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Greenshot.Helpers
{
	/// <summary>
	/// Soundhelper
	/// Create to fix the sometimes wrongly played sample, especially after first start from IDE
	/// See: http://www.codeproject.com/KB/audio-video/soundplayerbug.aspx?msg=2487569
	/// </summary>
	public static class SoundHelper
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof (SoundHelper));
		private static readonly ICoreConfiguration conf = IniConfig.Current.Get<ICoreConfiguration>();
		private static GCHandle? gcHandle = null;
		private static byte[] soundBuffer = null;

		public static void Initialize()
		{
			if (gcHandle == null)
			{
				try
				{
					ResourceManager resources = new ResourceManager("Greenshot.Sounds", Assembly.GetExecutingAssembly());
					soundBuffer = (byte[]) resources.GetObject("camera");

					if (conf.NotificationSound != null && conf.NotificationSound.EndsWith(".wav"))
					{
						try
						{
							if (File.Exists(conf.NotificationSound))
							{
								soundBuffer = File.ReadAllBytes(conf.NotificationSound);
							}
						}
						catch (Exception ex)
						{
							LOG.WarnFormat("couldn't load {0}: {1}", conf.NotificationSound, ex.Message);
						}
					}
					// Pin sound so it can't be moved by the Garbage Collector, this was the cause for the bad sound
					gcHandle = GCHandle.Alloc(soundBuffer, GCHandleType.Pinned);
				}
				catch (Exception e)
				{
					LOG.Error("Error initializing.", e);
				}
			}
		}

		/// <summary>
		/// Play the sound async (is wrapeed)
		/// </summary>
		/// <returns></returns>
		public static async Task Play(CancellationToken token = default(CancellationToken))
		{
			if (soundBuffer != null)
			{
				SoundFlags flags = SoundFlags.SND_ASYNC | SoundFlags.SND_MEMORY | SoundFlags.SND_NOWAIT | SoundFlags.SND_NOSTOP;
				try
				{
					await Task.Run(() => WinMM.PlaySound(gcHandle.Value.AddrOfPinnedObject(), (UIntPtr) 0, flags), token).ConfigureAwait(false);
				}
				catch (Exception e)
				{
					LOG.Error("Error in play.", e);
				}
			}
		}

		public static void Deinitialize()
		{
			try
			{
				if (gcHandle != null)
				{
					WinMM.PlaySound((byte[]) null, (UIntPtr) 0, (uint) 0);
					gcHandle.Value.Free();
					gcHandle = null;
				}
			}
			catch (Exception e)
			{
				LOG.Error("Error in deinitialize.", e);
			}
		}
	}
}