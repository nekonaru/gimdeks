# Changelog

Semua perubahan penting proyek ini didokumentasikan di sini. Formatnya berdasarkan
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), dan proyek ini mengikuti
[Semantic Versioning](https://semver.org/).

## v2.1.0

Rapikan koleksi dengan galeri screenshot, kelola backup, pencarian lanjutan, dan filter tersimpan.

## Fitur baru

- **Galeri screenshot.** Tambah gambar lokal dari jendela detail game, lihat thumbnail, dan hapusnya. File disimpan di folder covers lokal dan tidak pernah diunggah.
- **Kelola Backup.** Halaman khusus menampilkan backup lokal dengan tanggal, ukuran, serta pratinjau jumlah storage dan game. Buat backup, buka folder, atau pulihkan dengan konfirmasi dan salinan pengaman otomatis.
- **Pencarian lanjutan.** Cari berdasarkan nama dengan regular expression, catatan bertanggal, dan rentang ukuran dari satu dialog.
- **Filter tersimpan.** Simpan filter aktif dengan nama dan terapkan lagi dari daftar game.
- **Riwayat import.** Setiap import berhasil mencatat sumber, mode, dan hasilnya. Riwayat tampil di halaman Import / Export.
- **Pintasan storage.** Segarkan drive yang terhubung dan buka drive tertaut di Explorer dari halaman Storage.

## Peningkatan

- **Filter yang mengenali catatan.** Pencarian catatan lanjutan mencakup kolom Catatan yang kompatibel dengan Excel dan catatan bertanggal dari jendela detail.

## v2.0.0

Catat berapa lama kamu main, tandai favorit, dan temukan game atau aksi apa pun dengan cepat. Ini pembaruan mayor pertama dengan versi skema baru.

## Fitur baru

- **Sesi main.** Meluncurkan game memulai sesi yang berakhir saat proses berhenti. Durasi, waktu mulai, dan sumber disimpan. Sesi yang masih berjalan ditutup aman saat aplikasi keluar. Panel detail menampilkan daftar sesi dan total waktu.
- **Favorit, rating, dan prioritas.** Tiap game bisa ditandai favorit, diberi rating 0 sampai 5, dan memiliki prioritas unduhan. Daftar game memiliki kolom FAV dan RATE, serta pengurutan berdasarkan favorit, rating, prioritas, dan waktu main.
- **Panel detail game.** Jendela khusus menampilkan cover, catatan, sesi main, dan aksi langsung untuk luncurkan serta membuka folder game lokal.
- **Catatan bertanggal.** Simpan beberapa catatan per game dengan timestamp, terpisah dari kolom Catatan yang kompatibel dengan Excel.
- **Pencarian cepat.** Ctrl+P membuka command palette yang mencari semua game dan aksi umum dengan navigasi keyboard.
- **Folder game lokal.** Tetapkan folder per game dan bukakan di Explorer dari panel detail. Path tidak pernah diekspor.

## Peningkatan

- **Skema versi 4.** Menambah tabel dan kolom baru dengan backup otomatis sebelum upgrade yang sudah ada.
- **Edit game yang aman.** Mengedit game kini mempertahankan jumlah main, total waktu, dan timestamp alih-alih mengulangnya.
- **Tautan drive yang jujur.** Kegagalan menulis tautan drive dicatat dan dilaporkan, tidak lagi mengklaim berhasil.
- **Pilihan storage.** Load async yang usang dibuang memakai token seleksi, sehingga klik cepat tidak menampilkan data storage sebelumnya.
- **Pengaman import dan export.** Perintah yang berjalan lama tidak bisa tumpang tindih saat halaman sibuk.
- **Game dieliminasi di Excel.** Asal HDD dan Alasan kini memakai dua kolom, sehingga alasan tidak berubah menjadi storage palsu.

## Perbaikan bug

- **Waktu main terjaga.** Peluncuran tidak lagi menimpa kolom game tambahan, dan pelacakan sesi menggantikan jalur lama yang hanya menambah hitungan.
- **Deteksi Dikecualikan.** Sheet dengan kolom Alasan dan Asal tidak lagi salah diklasifikasikan sebagai Dikecualikan.

## v1.9.0

Pindai game terpasang di drive lokal dan tautkan executable ke koleksi, beserta banyak perbaikan data dan interaksi dari audit segar.

## Fitur baru

- **Pemindai game terpasang.** Halaman Pindai Game baru membaca file `.exe` dari drive atau folder pilihan, menyarankan tautan untuk game yang belum punya path peluncuran, dan menyimpan hanya saran yang kamu setujui. Cocokan persis ditandai Persis, sisanya Kemungkinan. Pemindaian hanya membaca file; penyimpanan hanya menulis path lokal. Tidak ada proses yang dijalankan.

## Peningkatan

- **Mode import yang stabil.** Mode import memakai nilai enum tetap, bukan label yang diterjemahkan, sehingga pergantian bahasa tidak bisa menurunkan Ganti Semua menjadi Gabung tanpa pemberitahuan.
- **Deep link yang andal.** Kartu storage di dashboard, pilihan acak, dan temuan Pemeriksaan kini mengantri target dan menerapkannya setelah halaman selesai dimuat. Aplikasi yang baru dibuka tidak lagi menampilkan semua game saat kartu storage diklik.
- **Reload terserialisasi.** Reload daftar game dan storage berjalan berurutan, sehingga refresh yang tumpang tindih tidak bisa mengembalikan filter atau pilihan lama. Mengosongkan filter kini memicu satu pembangunan ulang, bukan tujuh.
- **Hasil cover yang jujur.** Kegagalan unduh cover dihitung, dicatat, dan ditampilkan di status, bukan dibungkam.
- **Backup unik.** Nama file backup memuat sufiks unik, sehingga dua backup dalam detik yang sama tidak menimpa satu sama lain.

## Perbaikan bug

- **Keamanan restore.** Pembersihan backup tidak lagi bisa menghapus backup yang sedang dipulihkan.
- **Perbandingan Ringkasan.** Sheet yang ternormalisasi ke nama sama digabungkan alih-alih menyebabkan error, dan label paling panjang yang menang sehingga HDD 10 tidak mewarisi total HDD 1.
- **Import Excel.** Deteksi duplikat memakai normalizer nama game aplikasi di kedua mode. Nama sheet storage dicocokkan tanpa memperhatikan huruf besar dan memperbolehkan sufiks yang didokumentasikan. Game bernyata TOTAL tidak lagi memotong sheet. Jumlah Ringkasan dibatasi agar tidak overflow.
- **Roundtrip Excel.** Kapasitas drive disimpan dua desimal, sehingga drive 930 GB tidak kembali sebagai 900 GB.
- **Cache preview.** Cache menyertakan hash file dan dikosongkan setelah import berhasil, sehingga file yang ditulis ulang tidak menghasilkan data basi.
- **Konsistensi data.** Pemindahan storage tanpa perubahan tidak lagi menulis riwayat, dan pembuatan tag bersifat atomik saat dipanggil bersamaan.
- **Interaksi.** Peluncuran dilindungi dari klik ganda, pilihan daftar status dikosongkan saat reload, dan import drag and drop mencatat path yang dipantau. Label bahasa Inggris tersisa kini mengikuti bahasa aktif.

## v1.8.0

Periksa kesehatan koleksi sekilas, dan urutkan game berdasarkan seberapa sering dan kapan terakhir kamu memainkannya.

## Fitur baru

- **Halaman Pemeriksaan.** Halaman baru di sidebar memindai storage yang melebihi batas, drive yang tidak terhubung, grup duplikat, game berukuran nol, game dengan status tidak konsisten, backup yang belum ada atau sudah lama, dan file Excel sumber yang hilang. Setiap temuan punya tombol Buka yang langsung menuju halaman terkait dan memilih itemnya.
- **Pengurutan berdasarkan main.** Daftar game kini bisa diurutkan berdasarkan ukuran terbesar, paling sering dimainkan, baru dimainkan, atau status, selain urutan abjad bawaan.

## Peningkatan

- **Temuan yang bisa ditindaklanjuti.** Temuan pemeriksaan menuju ke Storage, Duplikat, Daftar Game, dan Import / Export, sehingga hasil pindai langsung mengarah ke perbaikan.
- **Tetap offline.** Semua pemeriksaan hanya membaca file SQLite lokal dan drive lokal. Tidak ada panggilan jaringan baru.

## v1.7.0

Luncurkan game yang sudah terpasang langsung dari koleksi, dan simpan catatan main yang sederhana.

## Fitur baru

- **Peluncur game lokal.** Isi path `.exe` game di editornya, lalu tekan Luncurkan dari toolbar, menu baris, atau klik kanan. Hanya file `.exe` lokal yang dipilih pengguna yang dijalankan, tanpa argumen dan tanpa shell.
- **Catatan main.** Setiap peluncuran berhasil menambah jumlah main dan mencatat waktunya. Daftar game menampilkan kolom DIMAINKAN dengan "Belum dimainkan" sebelum game pernah dijalankan.

## Peningkatan

- **Pemilih executable.** Editor game memiliki tombol Pilih File yang hanya menampilkan file `.exe`.
- **Migrasi skema.** Versi 3 menambah kolom path peluncuran dan catatan main dengan backup otomatis sebelum upgrade yang sudah ada.

## v1.6.1

Fitur yang ada sekarang berjalan lebih ringan. Cover, filter, import, dan pemindahan game tidak lagi mengulang kerja yang cukup dilakukan sekali.

## Peningkatan

- **Pencarian cover.** Folder cover diindeks sekali di memori, sehingga merender atau menggulir daftar game tidak memindai folder itu untuk setiap baris.
- **Filter.** Peta tag dimuat sekali per refresh, bukan sekali per ketikan pencarian.
- **Dashboard.** Game dikelompokkan sekali dan drive yang siap dicek dengan satu enumerasi, bukan satu per storage.
- **Import.** Pratinjau dipakai ulang untuk file dan timestamp yang sama. Deteksi duplikat memuat nama yang ada sekali, bukan query per game. Kalkulasi rumus dijalankan sekali per pratinjau, bukan per sel.
- **Pemindahan game.** Riwayat dan pembaruan storage ditulis dalam dua pernyataan, bukan tiga query per game.

## Perbaikan bug

- **Verifikasi Ringkasan.** Sheet tanpa baris yang bisa diverifikasi tidak lagi dilaporkan cocok, dan kegagalan kalkulasi rumus tampil sebagai peringatan.
- **Backup upgrade skema.** Salinan database sebelum upgrade dihapus hanya setelah migrasi berhasil, sehingga tidak menumpuk tanpa terlihat.

## v1.6.0

Satu klik untuk memilih game saat kamu bingung mau main apa.

## Fitur baru

- **Pilihan acak.** Kartu di dashboard memilih salah satu game Sudah Ada yang belum selesai secara acak, membuka daftar game dengan semua filter dikosongkan, menampilkan pilihan di status bar, serta memilih dan menggulir ke barisnya.

## Perbaikan bug

- **Pilihan baris saat berpindah halaman** sekarang diterapkan ulang ketika halaman dibuat atau dimuat ulang, sehingga navigasi terprogram dari dashboard menyisakan baris yang dimaksud dalam keadaan terpilih.

## v1.5.0

Menu klik kanan di baris game, dan satu klik untuk mengunduh semua cover yang belum ada.

## Fitur baru

- **Aksi klik kanan di baris game.** Ubah, pindahkan, ubah status, pisahkan versi, tandai selesai, unduh cover, atau hapus, semuanya dari menu klik kanan di baris game mana pun.
- **Unduh semua cover yang belum ada.** Satu tombol mencari setiap game tanpa cover lokal dan mengunduh cover Steam secara batch, dengan konfirmasi.
- **Penyesuaian layout.** Toolbar daftar game sekarang memuat kontrol aksi cover terdeteksi di samping aksi pilihannya sendiri.

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
