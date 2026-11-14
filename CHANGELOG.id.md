# Changelog

Semua perubahan penting proyek ini didokumentasikan di sini. Formatnya berdasarkan
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), dan proyek ini mengikuti
[Semantic Versioning](https://semver.org/).

## v1.0.0

Rilis publik pertama. GimDeks menggantikan spreadsheet Koleksi_Game.xlsx dengan aplikasi Windows offline yang mengimport file yang sama, melacak kapasitas otomatis, dan mengexportnya kembali.

## Fitur baru

- **Import dan export Excel bolak-balik.** Membaca seluruh format Koleksi_Game.xlsx berdasarkan pola isi, dan menulisnya kembali dengan format yang sama plus rumus total aktif.
- **Pratinjau import dengan pemeriksaan Ringkasan.** Menampilkan storage terdeteksi, game, sheet yang dilewati, dan membandingkan hasilnya dengan sheet Ringkasan sebelum disimpan.
- **Total kapasitas otomatis.** Jumlah game dan GB per storage, sisa ruang, dan progress bar menuju batas storage, dengan lencana peringatan saat melebihi batas (default 900 GB untuk HDD eksternal 1 TB).
- **Dashboard dan daftar game.** Dashboard satu kartu per storage dan tabel game bervirtualisasi yang bisa dicari dan difilter, dengan tambah, ubah, hapus, pindah dan ubah status, termasuk multi-pilih.
- **Kelola storage.** Tambah dan ubah storage (kapasitas, batas, tipe), lihat statistik per storage dan riwayat pemindahan.
- **Dikecualikan dan Dieliminasi.** Layar khusus dengan pemulihan ke storage; game yang dikecualikan tidak pernah dihitung ke total.
- **Deteksi duplikat.** Menemukan game kembar (huruf besar-kecil, tanda baca, nomor versi diabaikan) dan menghapus atau menandainya Dieliminasi per grup.
- **Saran pembagian HDD.** Memasukkan game Belum Download ke HDD kamu di bawah batas per drive, terbesar dulu.
- **Pemisahan versi.** Satu aksi memindahkan versi di akhir nama seperti `v1.7` ke kolom Versi.
- **Backup otomatis.** Backup database dibuat sebelum setiap import, ditambah backup dan restore sekali klik.
- **Tema gelap dan terang.** Dapat diganti dari sidebar; pilihan diingat.
- **Offline dan privat.** Satu file SQLite di `%LOCALAPPDATA%\GimDeks`. Tanpa akun, telemetri, atau panggilan jaringan.

## Peningkatan

- **Performa import.** File Excel asli selesai diimport dalam waktu kurang dari satu detik.

## Perbaikan bug

- Tidak ada (rilis pertama).
