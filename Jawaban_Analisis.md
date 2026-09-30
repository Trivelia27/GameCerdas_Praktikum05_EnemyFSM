# Praktikum 5 — Finite State Machine (Enemy AI)

Anggota: 
- Triana Velia Hutabalian (5025231190)
- Lailatul Annisa Fitriana (5025231202)
- Rafaela Shyra Ashma' Ramadhani (5025231217)


Bonus yang dikerjakan: **UI Current State** (HUD + label di atas Enemy + warna Enemy per state) — `EnemyStateUI.cs`.

## Analisis Singkat

**1. Apa perbedaan State, Condition, dan Transition?**
- **State**: perilaku/mode yang sedang dijalankan NPC pada satu waktu (Patrol, Chase, Attack, Flee, Dead).
- **Condition**: pernyataan benar/salah yang diperiksa, mis. `CanSeePlayer`, `distance <= attackRange`, `HP <= threshold`.
- **Transition**: perpindahan dari satu state ke state lain yang terjadi ketika suatu condition terpenuhi, mis. Patrol → Chase saat `CanSeePlayer == true`.

**2. Mengapa NavMeshAgent bukan merupakan FSM?**
NavMeshAgent hanya menjawab *bagaimana* bergerak menuju tujuan (pathfinding + steering). Ia tidak memutuskan *apa* yang harus dilakukan NPC, tidak punya state perilaku dan transition. FSM yang memutuskan tujuan (waypoint, Player, SafePoint) lalu memerintahkan NavMeshAgent lewat `SetDestination()`.

**3. Mengapa Line of Sight menggunakan Raycast?**
Raycast menembakkan garis dari mata Enemy ke Player. Bila garis mengenai collider pada layer Obstacle sebelum sampai ke Player, berarti ada penghalang sehingga Player tidak terlihat. Jarak dan sudut pandang saja tidak cukup karena tidak tahu ada tembok di antara keduanya.

**4. Mengapa Attack membutuhkan cooldown?**
`Update()` dipanggil tiap frame (mis. 60 kali/detik). Tanpa cooldown, Enemy menyerang setiap frame sehingga damage tidak masuk akal. Dengan `nextAttackTime = Time.time + attackCooldown`, serangan dibatasi (1,5 detik sekali).

**5. Mengapa Attack Range dan Attack Exit Range dibuat berbeda?**
Untuk hysteresis (masuk Attack pada jarak ≤ 2,0, keluar pada jarak > 2,75). Bila nilainya sama, Player yang berada tepat di batas jarak membuat Enemy bolak-balik Chase ↔ Attack setiap frame (state flickering). Zona toleransi antara 2,0–2,75 menstabilkan state.

**6. Mengapa transition menuju Dead harus memiliki prioritas tinggi?**
Dead adalah kondisi final dan berlaku dari state mana pun (global transition). Jika dievaluasi setelah transition lain, Enemy dengan HP 0 bisa masih menyerang atau lari pada frame yang sama. Dengan diperiksa pertama di `Update()` (sebelum Flee dan logika state), Enemy yang mati langsung berhenti total.
