﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Xunit;

namespace AdvancedSharpAdbClient.Tests
{
    public partial class SyncServiceTests
    {
        /// <summary>
        /// Tests the <see cref="SyncService.StatAsync(string, CancellationToken)"/> method.
        /// </summary>
        [Fact]
        public async void StatAsyncTest()
        {
            FileStatistics value = await RunTestAsync(
                OkResponses(2),
                NoResponseMessages,
                ["host:transport:169.254.109.177:5555", "sync:"],
                [(SyncCommand.STAT, "/fstab.donatello")],
                [SyncCommand.STAT],
                [[160, 129, 0, 0, 85, 2, 0, 0, 0, 0, 0, 0]],
                null,
                async () =>
                {
                    using SyncService service = new(Socket, Device);
                    return await service.StatAsync("/fstab.donatello");
                });

            Assert.Equal(UnixFileType.Regular, value.FileType & UnixFileType.TypeMask);
            Assert.Equal(597, value.Size);
            Assert.Equal(DateTimeExtensions.Epoch.ToLocalTime(), value.Time);
        }

        /// <summary>
        /// Tests the <see cref="SyncService.GetDirectoryListingAsync(string, CancellationToken)"/> method.
        /// </summary>
        [Fact]
        public async void GetListingAsyncTest()
        {
            List<FileStatistics> value = await RunTestAsync(
                OkResponses(2),
                [".", "..", "sdcard0", "emulated"],
                ["host:transport:169.254.109.177:5555", "sync:"],
                [(SyncCommand.LIST, "/storage")],
                [SyncCommand.DENT, SyncCommand.DENT, SyncCommand.DENT, SyncCommand.DENT, SyncCommand.DONE],
                [
                    [233, 65, 0, 0, 0, 0, 0, 0, 152, 130, 56, 86],
                    [237, 65, 0, 0, 0, 0, 0, 0, 152, 130, 56, 86],
                    [255, 161, 0, 0, 24, 0, 0, 0, 152, 130, 56, 86],
                    [109, 65, 0, 0, 0, 0, 0, 0, 152, 130, 56, 86]
                ],
                null,
                async () =>
                {
                    using SyncService service = new(Socket, Device);
                    return await service.GetDirectoryListingAsync("/storage");
                });

            Assert.Equal(4, value.Count);

            DateTime time = new DateTime(2015, 11, 3, 9, 47, 4, DateTimeKind.Utc).ToLocalTime();

            FileStatistics dir = value[0];
            Assert.Equal(".", dir.Path);
            Assert.Equal((UnixFileType)16873, dir.FileType);
            Assert.Equal(0, dir.Size);
            Assert.Equal(time, dir.Time);

            FileStatistics parentDir = value[1];
            Assert.Equal("..", parentDir.Path);
            Assert.Equal((UnixFileType)16877, parentDir.FileType);
            Assert.Equal(0, parentDir.Size);
            Assert.Equal(time, parentDir.Time);

            FileStatistics sdcard0 = value[2];
            Assert.Equal("sdcard0", sdcard0.Path);
            Assert.Equal((UnixFileType)41471, sdcard0.FileType);
            Assert.Equal(24, sdcard0.Size);
            Assert.Equal(time, sdcard0.Time);

            FileStatistics emulated = value[3];
            Assert.Equal("emulated", emulated.Path);
            Assert.Equal((UnixFileType)16749, emulated.FileType);
            Assert.Equal(0, emulated.Size);
            Assert.Equal(time, emulated.Time);
        }

        /// <summary>
        /// Tests the <see cref="SyncService.GetDirectoryAsyncListing(string, CancellationToken)"/> method.
        /// </summary>
        [Fact]
        public async void GetAsyncListingTest()
        {
            List<FileStatistics> value = await RunTestAsync(
                OkResponses(2),
                [".", "..", "sdcard0", "emulated"],
                ["host:transport:169.254.109.177:5555", "sync:"],
                [(SyncCommand.LIST, "/storage")],
                [SyncCommand.DENT, SyncCommand.DENT, SyncCommand.DENT, SyncCommand.DENT, SyncCommand.DONE],
                [
                    [233, 65, 0, 0, 0, 0, 0, 0, 152, 130, 56, 86],
                    [237, 65, 0, 0, 0, 0, 0, 0, 152, 130, 56, 86],
                    [255, 161, 0, 0, 24, 0, 0, 0, 152, 130, 56, 86],
                    [109, 65, 0, 0, 0, 0, 0, 0, 152, 130, 56, 86]
                ],
                null,
                async () =>
                {
                    using SyncService service = new(Socket, Device);
                    return await service.GetDirectoryAsyncListing("/storage").ToListAsync();
                });

            Assert.Equal(4, value.Count);

            DateTime time = new DateTime(2015, 11, 3, 9, 47, 4, DateTimeKind.Utc).ToLocalTime();

            FileStatistics dir = value[0];
            Assert.Equal(".", dir.Path);
            Assert.Equal((UnixFileType)16873, dir.FileType);
            Assert.Equal(0, dir.Size);
            Assert.Equal(time, dir.Time);

            FileStatistics parentDir = value[1];
            Assert.Equal("..", parentDir.Path);
            Assert.Equal((UnixFileType)16877, parentDir.FileType);
            Assert.Equal(0, parentDir.Size);
            Assert.Equal(time, parentDir.Time);

            FileStatistics sdcard0 = value[2];
            Assert.Equal("sdcard0", sdcard0.Path);
            Assert.Equal((UnixFileType)41471, sdcard0.FileType);
            Assert.Equal(24, sdcard0.Size);
            Assert.Equal(time, sdcard0.Time);

            FileStatistics emulated = value[3];
            Assert.Equal("emulated", emulated.Path);
            Assert.Equal((UnixFileType)16749, emulated.FileType);
            Assert.Equal(0, emulated.Size);
            Assert.Equal(time, emulated.Time);
        }

        /// <summary>
        /// Tests the <see cref="SyncService.PullAsync(string, Stream, Action{SyncProgressChangedEventArgs}?, CancellationToken)"/> method.
        /// </summary>
        [Fact]
        public async void PullAsyncTest()
        {
            await using MemoryStream stream = new();
            byte[] content = await File.ReadAllBytesAsync("Assets/Fstab.bin");
            byte[] contentLength = BitConverter.GetBytes(content.Length);

            await RunTestAsync(
                OkResponses(2),
                NoResponseMessages,
                ["host:transport:169.254.109.177:5555", "sync:"],
                [
                    (SyncCommand.STAT, "/fstab.donatello"),
                    (SyncCommand.RECV, "/fstab.donatello")
                ],
                [SyncCommand.STAT, SyncCommand.DATA, SyncCommand.DONE],
                [
                    [160, 129, 0, 0, 85, 2, 0, 0, 0, 0, 0, 0],
                    contentLength,
                    content
                ],
                null,
                async () =>
                {
                    using SyncService service = new(Socket, Device);
                    await service.PullAsync("/fstab.donatello", stream, null);
                });

            // Make sure the data that has been sent to the stream is the expected data
            Assert.Equal(content, stream.ToArray());
        }

        /// <summary>
        /// Tests the <see cref="SyncService.PushAsync(Stream, string, int, DateTimeOffset, Action{SyncProgressChangedEventArgs}?, CancellationToken)"/> method.
        /// </summary>
        [Fact]
        public async void PushAsyncTest()
        {
            FileStream stream = File.OpenRead("Assets/Fstab.bin");
            byte[] content = await File.ReadAllBytesAsync("Assets/Fstab.bin");
            byte[] contentMessage =
            [
                .. SyncCommand.DATA.GetBytes(),
                .. BitConverter.GetBytes(content.Length),
                .. content,
            ];

            await RunTestAsync(
                OkResponses(2),
                NoResponseMessages,
                ["host:transport:169.254.109.177:5555", "sync:"],
                [
                    (SyncCommand.SEND, "/sdcard/test,644"),
                    (SyncCommand.DONE, "1446505200")
                ],
                [SyncCommand.OKAY],
                null,
                [contentMessage],
                async () =>
                {
                    using SyncService service = new(Socket, Device);
                    await service.PushAsync(stream, "/sdcard/test", 0644, new DateTime(2015, 11, 2, 23, 0, 0, DateTimeKind.Utc), null);
                });
        }

#if WINDOWS10_0_17763_0_OR_GREATER
        /// <summary>
        /// Tests the <see cref="SyncService.PullAsync(string, IOutputStream, Action{SyncProgressChangedEventArgs}?, CancellationToken)"/> method.
        /// </summary>
        [Fact]
        public async void PullWinRTAsyncTest()
        {
            using InMemoryRandomAccessStream stream = new();
            byte[] content = await File.ReadAllBytesAsync("Assets/Fstab.bin");
            byte[] contentLength = BitConverter.GetBytes(content.Length);

            await RunTestAsync(
                OkResponses(2),
                NoResponseMessages,
                ["host:transport:169.254.109.177:5555", "sync:"],
                [
                    (SyncCommand.STAT, "/fstab.donatello"),
                    (SyncCommand.RECV, "/fstab.donatello")
                ],
                [SyncCommand.STAT, SyncCommand.DATA, SyncCommand.DONE],
                [
                    [160, 129, 0, 0, 85, 2, 0, 0, 0, 0, 0, 0],
                    contentLength,
                    content
                ],
                null,
                async () =>
                {
                    using SyncService service = new(Socket, Device);
                    await service.PullAsync("/fstab.donatello", stream, null);
                });

            IBuffer buffer = await stream.GetInputStreamAt(0).ReadAsync(new byte[(int)stream.Size].AsBuffer(), (uint)stream.Size, InputStreamOptions.None);
            // Make sure the data that has been sent to the stream is the expected data
            Assert.Equal(content, buffer.ToArray());
        }

        /// <summary>
        /// Tests the <see cref="SyncService.PullAsync(string, IOutputStream, Action{SyncProgressChangedEventArgs}?, CancellationToken)"/> method.
        /// </summary>
        [Fact]
        public async void PushWinRTAsyncTest()
        {
            StorageFile storageFile = await StorageFile.GetFileFromPathAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Fstab.bin"));
            using IRandomAccessStreamWithContentType stream = await storageFile.OpenReadAsync();
            byte[] content = await File.ReadAllBytesAsync("Assets/Fstab.bin");
            byte[] contentMessage =
            [
                .. SyncCommand.DATA.GetBytes(),
                .. BitConverter.GetBytes(content.Length),
                .. content,
            ];

            await RunTestAsync(
                OkResponses(2),
                NoResponseMessages,
                ["host:transport:169.254.109.177:5555", "sync:"],
                [
                    (SyncCommand.SEND, "/sdcard/test,644"),
                    (SyncCommand.DONE, "1446505200")
                ],
                [SyncCommand.OKAY],
                null,
                [contentMessage],
                async () =>
                {
                    using SyncService service = new(Socket, Device);
                    await service.PushAsync(stream, "/sdcard/test", 0644, new DateTime(2015, 11, 2, 23, 0, 0, DateTimeKind.Utc), null);
                });
        }
#endif
    }
}
