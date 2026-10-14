# SonmezRacePlugin - Kurulum (AssettoServer 0.0.55 preview 44, net9.0)

## GitHub'da derleme
1. Yeni bir GitHub deposu ac, bu klasorun icindekileri deponun kokune yukle
   (`SonmezRacePlugin.csproj` ve `.github/workflows/build.yml` kokte olmali).
2. Repo > **Actions** sekmesi > "Build SonmezRacePlugin" kendiliginden calisir
   (veya **Run workflow**).
3. Bitince calismanin altindaki **Artifacts** > `SonmezRacePlugin-0.0.55-pre44` indir.
   Hata olursa `build-log` artifact'i ve Actions logu hatayi gosterir.
4. Surum etiketi atarsan (ornek: `git tag v1.0 && git push --tags`) zip otomatik **Releases**'e eklenir.

## Sunucuya kurulum
1. Zip'i ac. `plugins/SonmezRacePlugin/SonmezRacePlugin.dll` dosyasini sunucudaki
   `plugins/SonmezRacePlugin/` klasorune kopyala.
2. `cfg/extra_cfg.yml` icine ekle:
   ```
   EnablePlugins:
   - SonmezRacePlugin
   EnableClientMessages: true
   ```
3. (Opsiyonel) `cfg/plugin_sonmez_race_cfg.yml` ayarlari degistirmek icin.
4. CSP gerekli (online scriptler icin).

## Sorun giderme
- Drift acisi ters/yanlis cikarsa config'de `YawMode: 1..8` dene.
- Korna algilanmazsa Actions'ta **Run workflow** > `with_commands` isaretle, oyunda `/yaris` yaz.
- Farkli bir AssettoServer surumu icin Run workflow > `server_ref` alanina etiketi yaz.
