# Changelog

Semua perubahan penting proyek ini didokumentasikan di sini. Formatnya berdasarkan
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), dan proyek ini mengikuti
[Semantic Versioning](https://semver.org/).

## v1.4.0

Penyegaran visual di seluruh aplikasi: permukaan gradien lebih gelap, kartu dengan aksen bercahaya, dan sidebar yang berubah, plus beberapa perbaikan di balik layar.

## Fitur baru

- **Aksen sekunder yang fokus.** Item navigasi yang aktif membawa bar aksen teal dan latar pill teal lembut; state hover bersinar halus.
- **Tampilan pertama dashboard.** Layar kosong sekarang menampilkan wordmark GimDeks di tengah bersama prompt untuk mengimpor koleksi kamu.
- **Penekanan opsi.** Kartu storage di dashboard membuka daftar game lewat klik di seluruh kartu.

## Peningkatan

- **Penyegaran visual.** Sidebar memakai palet gradien lebih gelap; kartu memakai isi gradien dengan garis aksen 1-pixel; hierarki jenis huruf lebih rapi; judul memakai semi-bold 30px.
- **Paket aset.** Semua aset brand (lockup, mark, ikon, screenshot) sekarang ikut dalam rilis biner.

## v1.3.0

Pilihan kamu bertahan saat refresh, upgrade skema menyimpan backup pengaman, dan koleksimu bisa diexport ke CSV.

## Fitur baru

- **Pilihan bertahan saat refresh.** Hapus, pindahkan, ubah status, atau tandai selesai: baris yang masih terpilih tetap terpilih setelah reload, di daftar dan untuk semua aksi massal.
- **Backup sebelum migrasi.** Saat upgrade skema menunggu, aplikasi menyimpan salinan `gimdeks-pre-upgrade-<timestamp>.db` di folder backups sebelum menjalankan migrasi.
- **Export CSV.** Seluruh koleksi diexport ke CSV UTF-8 dengan BOM (dipisah titik koma), terbuka langsung di kedua bahasa Excel. Datanya sama dengan export Excel, dengan nama storage dan daftar tag per game.

## Perbaikan bug

- **Tandai selesai kehilangan pilihan.** Rebuild grid setelah reload mengganti objek baris, sehingga pilihan dihapus sesaat setelah toggle; aksi massal tidak mengenai apapun. Pilihan sekarang bertahan.
- **Balapan pembuatan filter.** Pergantian filter yang terpicu dua kali per reload bisa memulihkan hanya sebagian pilihan (satu baris hilang). Semua jalur rebuild sekarang diserialisasi.
- **Item daftar storage punya nama aksesibilitas.**

## v1.2.0

Perbaikan menyeluruh interaksi: tab sidebar berfungsi lagi, aplikasi mengingat posisi kamu terakhir, dan Koleksi_Game.xlsx bisa diimpor dengan menyeretnya ke jendela.

## Fitur baru

- **Impor dengan drag and drop.** Seret Koleksi_Game.xlsx ke mana pun di jendela: path terisi otomatis dan pratinjau terbuka.
- **Klik dua kali untuk ubah.** Klik dua kali baris game untuk membuka editornya.
- **Tandai selesai secara massal.** Pilih beberapa game sekaligus dan centang flag selesai untuk semuanya.
- **Mengingat posisi terakhir.** Aplikasi terbuka lagi di halaman terakhir dan memulihkan posisi serta ukuran jendela (dengan pembatas di luar layar).
- **Nama aksesibilitas.** Item navigasi dan tombol berikon dikenali screen reader dan Windows UI Automation.

## Peningkatan

- **Kartu dashboard ramah keyboard.** Kartu storage sekarang tombol sungguhan dengan aktivasi keyboard dan nama aksesibel.
- **Judul jendela dan baris versi.** Title bar menampilkan halaman aktif, dan baris versi sidebar diambil dari assembly, bukan string hardcoded.
- **Kegagalan terlihat.** Refresh halaman yang gagal tercatat di gimdeks.log dan tampil di status bar, bukan diam.
- **Log crash me-redact path.** Path berhuruf drive dihilangkan dari log crash.
- **Cover di-cache.** Cover disimpan di memori, sehingga scroll daftar lebih halus.

## Perbaikan bug

- **Navigasi sidebar.** Tab sidebar sekarang mengganti halaman. Di v1.0.0 dan v1.1.0, klik tab tidak pernah berpindah (masalah mode binding pada view model navigasi); hanya pintasan Ctrl+1..9 yang berfungsi.
- **Toolbar tanpa pilihan** kini nonaktif sampai ada pilihan, bukan diam-diam melewati klik.
- **Pengaturan saat jendela ditutup** tidak lagi menghapus bahasa, tema, dan path file hasil import.
- **Pemantau file Excel** dihentikan saat jendela ditutup dan dibersihkan dengan benar saat bahasa diganti.

## v1.1.0

GimDeks sekarang bisa berbicara Bahasa Inggris selain Bahasa Indonesia, meminta izin sebelum setiap aksi online, dan lebih aman terhadap kerusakan data: restore yang lebih aman, mode portabel yang lebih aman, gerbang rilis yang lebih jelas.

## Fitur baru

- **Antarmuka dua bahasa.** Bahasa Indonesia tetap bahasa bawaan; seluruh aplikasi bisa diganti ke bahasa Inggris dari tombol sidebar, dan pilihannya diingat. Angka mengikuti bahasa yang aktif.
- **Halaman Statistik.** Total koleksi, porsi per storage, 10 game terbesar, rata-rata ukuran, dan kemajuan penyelesaian.
- **Tag dan penyelesaian.** Tag buatan sendiri, dipisah koma, dan tanda selesai per game; keduanya bisa difilter; statistik penyelesaian ada di halaman statistik.
- **Cover Steam (opsional).** Satu klik mencari game di Steam dan mengunduh header art sebagai cover. Yang dikirim hanya judul game, dan tidak ada yang terjadi tanpa klik kamu.
- **Mode portabel.** Satu tombol memindahkan folder data ke sebelah aplikasi; database dan pengaturan ikut berpindah bersama drive eksternal.
- **Tautan drive.** Hubungkan storage ke huruf drive; dashboard menampilkan lencana saat drive tidak terhubung.
- **Pemantau import.** Spanduk menawarkan import ulang saat file Excel yang diimport berubah di disk.
- **Pintasan keyboard.** Ctrl+1 sampai Ctrl+9 ganti halaman, Ctrl+F cari, Ctrl+N tambah game, Ctrl+E export, Ctrl+T ganti tema.

## Peningkatan

- **Polesan antarmuka.** Transisi halaman memudar, sidebar lebih gelap dengan bilah aksen pada item aktif, dan status bar berisi petunjuk pintasan.
- **Format angka dan tanggal** kini mengikuti bahasa UI yang aktif di seluruh tempat, termasuk layar storage.
- **Restore bersifat atomik.** Database disalin ke file sementara lalu diganti, sehingga restore yang gagal tidak meninggalkan file setengah tertulis.
- **Mode portabel mati lebih aman.** Salinan profil yang ada disimpan sebagai backup bertanda waktu (dipulihkan otomatis saat gagal) dan jumlah serta ukuran file tersalin diverifikasi sebelum sumber dihapus.
- **Pelindung import Excel.** Batas dekompresi (256 MB) sebelum parsing; arsip rusak ditolak sebelum dibuka.
- **CI dan workflow rilis** kini gagal di atas paket yang rentan, di-parse dari laporan machine-readable, bukan pencocokan teks, dan workflow rilis menjalankan audit yang sama sebelum packaging.

## Perbaikan bug

- **Pengaturan saat jendela ditutup** tidak lagi menghapus bahasa, tema, dan path file hasil import.
- **Dashboard termuat saat pertama dibuka**, bukan menampilkan keadaan kosong sampai navigasi pertama.
- **Label navigasi tampil dengan benar** setelah mengganti bahasa.
- **Pemantau file** tidak lagi berisiko menabrakkan aplikasi saat ditutup.

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
