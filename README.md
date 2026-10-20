# Hush

Hush is a lightweight command-line tool for uploading and receiving files through Rubis or custom HTTP endpoints.

Files are compressed using Zstandard when beneficial and can optionally be encrypted locally using AES-256-GCM with Argon2id key derivation. Without encryption, Hush uses Base64 encoding for transport.

Hush supports arbitrary file formats and aims to preserve original file contents during transfer.

Built with C# and .NET.

