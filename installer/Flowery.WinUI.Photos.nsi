Unicode true
ManifestDPIAware true
RequestExecutionLevel user
SetCompressor /SOLID lzma
Name "Flowery Photos"
OutFile "${OUTPUT_FILE}"
InstallDir "$LOCALAPPDATA\Programs\Flowery Photos"
InstallDirRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Flowery.WinUI.Photos" "InstallLocation"
VIProductVersion "${PRODUCT_VERSION}.0"
VIAddVersionKey /LANG=1033 "ProductName" "Flowery Photos"
VIAddVersionKey /LANG=1033 "ProductVersion" "${PRODUCT_VERSION}"
VIAddVersionKey /LANG=1033 "FileVersion" "${PRODUCT_VERSION}"
VIAddVersionKey /LANG=1033 "FileDescription" "Flowery Photos Setup"
VIAddVersionKey /LANG=1033 "LegalCopyright" "Copyright 2026 tobitege"

!include MUI2.nsh
!include LogicLib.nsh
!include FileFunc.nsh
!include TextFunc.nsh
!include x64.nsh
!define UNINSTALL_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\Flowery.WinUI.Photos"
!define APP_EXE "Flowery.WinUI.Photos.exe"
!define MUI_LANGDLL_ALLLANGUAGES
!define MUI_ICON "${__FILEDIR__}\..\Flowery.WinUI.Gallery\app_icon.ico"
!define MUI_UNICON "${__FILEDIR__}\..\Flowery.WinUI.Gallery\app_icon.ico"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "German"
!insertmacro MUI_LANGUAGE "French"
!insertmacro MUI_LANGUAGE "Spanish"
!insertmacro MUI_LANGUAGE "Italian"
!insertmacro MUI_LANGUAGE "Japanese"
!insertmacro MUI_LANGUAGE "Korean"
!insertmacro MUI_LANGUAGE "SimpChinese"
!insertmacro MUI_LANGUAGE "Arabic"
!insertmacro MUI_LANGUAGE "Turkish"
!insertmacro MUI_LANGUAGE "Ukrainian"
!insertmacro MUI_LANGUAGE "Hebrew"

LangString FileError ${LANG_ENGLISH} "Cannot update or remove the installed files. Close Flowery Photos and try again."
LangString FileError ${LANG_GERMAN} "Die installierten Dateien können nicht aktualisiert oder entfernt werden. Schließe Flowery Photos und versuche es erneut."
LangString FileError ${LANG_FRENCH} "Impossible de mettre à jour ou de supprimer les fichiers installés. Fermez Flowery Photos et réessayez."
LangString FileError ${LANG_SPANISH} "No se pueden actualizar ni eliminar los archivos instalados. Cierra Flowery Photos e inténtalo de nuevo."
LangString FileError ${LANG_ITALIAN} "Impossibile aggiornare o rimuovere i file installati. Chiudi Flowery Photos e riprova."
LangString FileError ${LANG_JAPANESE} "インストール済みファイルを更新または削除できません。Flowery Photos を閉じて再試行してください。"
LangString FileError ${LANG_KOREAN} "설치된 파일을 업데이트하거나 제거할 수 없습니다. Flowery Photos를 닫고 다시 시도하세요."
LangString FileError ${LANG_SIMPCHINESE} "无法更新或删除已安装的文件。请关闭 Flowery Photos 后重试。"
LangString FileError ${LANG_ARABIC} "تعذر تحديث الملفات المثبتة أو إزالتها. أغلق Flowery Photos وحاول مرة أخرى."
LangString FileError ${LANG_TURKISH} "Yüklü dosyalar güncellenemiyor veya kaldırılamıyor. Flowery Photos’u kapatıp yeniden deneyin."
LangString FileError ${LANG_UKRAINIAN} "Не вдалося оновити або видалити встановлені файли. Закрийте Flowery Photos та спробуйте ще раз."
LangString FileError ${LANG_HEBREW} "לא ניתן לעדכן או להסיר את הקבצים המותקנים. יש לסגור את Flowery Photos ולנסות שוב."

LangString SetupBusy ${LANG_ENGLISH} "Another Flowery Photos installation or removal is running. Wait for it to finish and try again."
LangString SetupBusy ${LANG_GERMAN} "Eine andere Installation oder Deinstallation von Flowery Photos läuft. Warte, bis sie beendet ist, und versuche es erneut."
LangString SetupBusy ${LANG_FRENCH} "Une autre installation ou désinstallation de Flowery Photos est en cours. Attendez sa fin et réessayez."
LangString SetupBusy ${LANG_SPANISH} "Hay otra instalación o desinstalación de Flowery Photos en curso. Espera a que termine e inténtalo de nuevo."
LangString SetupBusy ${LANG_ITALIAN} "È in corso un'altra installazione o disinstallazione di Flowery Photos. Attendi il completamento e riprova."
LangString SetupBusy ${LANG_JAPANESE} "別の Flowery Photos のインストールまたはアンインストールが実行中です。完了してから再試行してください。"
LangString SetupBusy ${LANG_KOREAN} "다른 Flowery Photos 설치 또는 제거가 진행 중입니다. 완료될 때까지 기다린 후 다시 시도하세요."
LangString SetupBusy ${LANG_SIMPCHINESE} "另一个 Flowery Photos 安装或卸载正在进行中。请等待完成后重试。"
LangString SetupBusy ${LANG_ARABIC} "تجري عملية أخرى لتثبيت Flowery Photos أو إزالته. انتظر حتى تنتهي ثم حاول مرة أخرى."
LangString SetupBusy ${LANG_TURKISH} "Başka bir Flowery Photos yükleme veya kaldırma işlemi sürüyor. Bitmesini bekleyip yeniden deneyin."
LangString SetupBusy ${LANG_UKRAINIAN} "Триває інше встановлення або видалення Flowery Photos. Дочекайтеся завершення та спробуйте ще раз."
LangString SetupBusy ${LANG_HEBREW} "התקנה או הסרה אחרת של Flowery Photos מתבצעת כעת. יש להמתין לסיומה ולנסות שוב."

Var Manifest
Var Handle
Var Entry
Var Parent
Var Position
Var Previous
Var Character
Var InitialCulture
Var StateDirectory
Var Transaction
Var Stage
Var Backup
Var Journal
Var TransactionFailed
Var RollbackFailed
Var SetupMutex

; Installation and removal share registration and shortcuts even across target
; directories. Keep one owner until process exit, including rollback.
; Global also covers another Windows session using the same installation.
!macro AcquireSetupLock
    System::Call 'kernel32::CreateMutexW(p 0, i 0, w "Global\Flowery.WinUI.Photos.Setup") p.r0'
    StrCpy $SetupMutex $0
    StrCpy $1 -1
    ${If} $SetupMutex != 0
        System::Call 'kernel32::WaitForSingleObject(p r0, i 0) i.r1'
    ${EndIf}
    ${If} $1 != 0
    ${AndIf} $1 != 128 ; WAIT_ABANDONED grants ownership after a terminated setup.
        MessageBox MB_ICONSTOP "$(SetupBusy)" /SD IDOK
        SetErrorLevel 1618
        Quit
    ${EndIf}
!macroend

; A relative manifest path can still escape through a junction in a parent.
; The selected installation root is the boundary; never follow links below it.
!macro RejectLinkedParents FailureLabel
    StrCpy $Parent "$Entry"
    ${Do}
        ${GetParent} "$Parent" $Parent
        ${If} $Parent == ""
            ${ExitDo}
        ${EndIf}
        System::Call 'kernel32::GetFileAttributesW(w "$INSTDIR\$Parent") i.r0'
        ${If} $0 != -1
            IntOp $0 $0 & 0x400 ; FILE_ATTRIBUTE_REPARSE_POINT
            ${If} $0 != 0
                Goto ${FailureLabel}
            ${EndIf}
        ${EndIf}
    ${Loop}
!macroend

; Accept only relative paths below the installation root.
!macro ValidateEntry FailureLabel
    StrCpy $0 $Entry 1
    StrCmp $0 "\" ${FailureLabel}
    StrCpy $Position 0
    StrCpy $Previous ""
    ${Do}
        StrCpy $Character $Entry 1 $Position
        ${If} $Character == ""
            ${ExitDo}
        ${EndIf}
        StrCmp $Character ":" ${FailureLabel}
        StrCmp $Character "/" ${FailureLabel}
        StrCmp $Character "*" ${FailureLabel}
        StrCmp $Character "?" ${FailureLabel}
        ${If} $Character == "."
        ${AndIf} $Previous == "."
            Goto ${FailureLabel}
        ${EndIf}
        StrCpy $Previous $Character
        IntOp $Position $Position + 1
    ${Loop}
    !insertmacro RejectLinkedParents ${FailureLabel}
!macroend

Section "Flowery Photos" SecApp
    SectionIn RO
    ; Stage on the destination volume before changing any existing payload.
    CreateDirectory "$INSTDIR"
    ClearErrors
    GetTempFileName $Transaction "$INSTDIR"
    IfErrors install_failed
    Delete "$Transaction"
    CreateDirectory "$Transaction"
    IfErrors install_failed
    StrCpy $Stage "$Transaction\new"
    StrCpy $Backup "$Transaction\old"
    StrCpy $TransactionFailed 0
    ClearErrors
    SetOverwrite on
    !include "${BUILD_DIR}\payload.nsh"
    SetOutPath "$Stage"
    File /oname=files.lst "${BUILD_DIR}\payload.lst"
    WriteUninstaller "$Stage\Uninstall.exe"
    IfErrors install_failed
    FileOpen $Journal "$Transaction\entries.lst" w
    IfErrors install_failed
    FileWriteUTF16LE /BOM $Journal ""
    StrCpy $Manifest "$INSTDIR\files.lst"
    Call AppendManifest
    StrCpy $Manifest "$Stage\files.lst"
    Call AppendManifest
    FileWriteUTF16LE $Journal "files.lst$\r$\nUninstall.exe$\r$\n"
    IfErrors install_failed
    FileClose $Journal
    StrCpy $Journal ""
    StrCmp $TransactionFailed 0 0 install_failed
    Call BackupFiles
    StrCmp $TransactionFailed 0 0 install_failed
    FileOpen $Journal "$Transaction\published.lst" w
    IfErrors install_failed
    FileWriteUTF16LE /BOM $Journal ""
    StrCpy $Manifest "$Stage\files.lst"
    Call PublishFiles
    StrCpy $Entry "files.lst"
    Call PublishEntry
    StrCpy $Entry "Uninstall.exe"
    Call PublishEntry
    FileClose $Journal
    StrCpy $Journal ""
    StrCmp $TransactionFailed 0 0 install_failed
    SetOutPath "$INSTDIR"
    CreateDirectory "$SMPROGRAMS\Flowery Photos"
    CreateShortcut "$SMPROGRAMS\Flowery Photos\Flowery Photos.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\${APP_EXE}" 0
    WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayName" "Flowery Photos"
    WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
    WriteRegStr HKCU "${UNINSTALL_KEY}" "Publisher" "tobitege"
    WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayIcon" "$INSTDIR\${APP_EXE}"
    WriteRegStr HKCU "${UNINSTALL_KEY}" "InstallLocation" "$INSTDIR"
    WriteRegStr HKCU "${UNINSTALL_KEY}" "UninstallString" '$\"$INSTDIR\Uninstall.exe$\"'
    WriteRegStr HKCU "${UNINSTALL_KEY}" "QuietUninstallString" '$\"$INSTDIR\Uninstall.exe$\" /S'
    WriteRegDWORD HKCU "${UNINSTALL_KEY}" "NoModify" 1
    WriteRegDWORD HKCU "${UNINSTALL_KEY}" "NoRepair" 1
    WriteRegDWORD HKCU "${UNINSTALL_KEY}" "Language" $LANGUAGE
    Call InitializeUserLanguage
    Call CleanTransaction
    Goto app_done
install_failed:
    StrCmp $Journal "" +2
    FileClose $Journal
    SetOutPath "$INSTDIR"
    Call RollbackFiles
    MessageBox MB_ICONSTOP "$(FileError)" /SD IDOK
    SetErrorLevel 1
    Quit
app_done:
SectionEnd

; Build the complete, validated target list before any rename. The existing
; manifest and the uninstaller are included in the same transaction as the payload.
Function AppendManifest
    IfFileExists "$Manifest" 0 append_done
    ClearErrors
    FileOpen $Handle "$Manifest" r
    IfErrors append_failed
    FileSeek $Handle 2 SET
append_next:
    ClearErrors
    FileReadUTF16LE $Handle $Entry
    IfErrors append_complete
    ${TrimNewLines} $Entry $Entry
    StrCmp $Entry "" append_next
    !insertmacro ValidateEntry append_invalid
    FileWriteUTF16LE $Journal "$Entry$\r$\n"
    IfErrors append_invalid
    Goto append_next
append_complete:
    FileClose $Handle
    Goto append_done
append_invalid:
    FileClose $Handle
append_failed:
    StrCpy $TransactionFailed 1
append_done:
FunctionEnd

Function BackupFiles
    FileOpen $Handle "$Transaction\entries.lst" r
    IfErrors backup_failed
    FileSeek $Handle 2 SET
backup_next:
    ClearErrors
    FileReadUTF16LE $Handle $Entry
    IfErrors backup_complete
    ${TrimNewLines} $Entry $Entry
    StrCmp $Entry "" backup_next
    IfFileExists "$Backup\$Entry" backup_next
    System::Call 'kernel32::GetFileAttributesW(w "$INSTDIR\$Entry") i.r0'
    IntCmp $0 -1 backup_next
    IntOp $0 $0 & 0x410 ; Reject directories and reparse points.
    IntCmp $0 0 0 backup_invalid backup_invalid
    ${GetParent} "$Backup\$Entry" $Parent
    CreateDirectory "$Parent"
    ClearErrors
    Rename "$INSTDIR\$Entry" "$Backup\$Entry"
    IfErrors backup_invalid
    Goto backup_next
backup_complete:
    FileClose $Handle
    Return
backup_invalid:
    FileClose $Handle
backup_failed:
    StrCpy $TransactionFailed 1
FunctionEnd

Function PublishFiles
    FileOpen $Handle "$Manifest" r
    IfErrors publish_failed
    FileSeek $Handle 2 SET
publish_next:
    ClearErrors
    FileReadUTF16LE $Handle $Entry
    IfErrors publish_complete
    ${TrimNewLines} $Entry $Entry
    StrCmp $Entry "" publish_next
    Call PublishEntry
    StrCmp $TransactionFailed 0 publish_next
publish_complete:
    FileClose $Handle
    Return
publish_failed:
    StrCpy $TransactionFailed 1
FunctionEnd

Function PublishEntry
    StrCmp $TransactionFailed 0 0 publish_entry_done
    ${GetParent} "$INSTDIR\$Entry" $Parent
    CreateDirectory "$Parent"
    ClearErrors
    ; Journal first: rollback only moves entries whose staged file is absent.
    FileWriteUTF16LE $Journal "$Entry$\r$\n"
    IfErrors publish_entry_failed
    Rename "$Stage\$Entry" "$INSTDIR\$Entry"
    IfErrors publish_entry_failed
    Return
publish_entry_failed:
    StrCpy $TransactionFailed 1
publish_entry_done:
FunctionEnd

Function RollbackFiles
    StrCpy $RollbackFailed 0
    StrCmp $Transaction "" rollback_return
    IfFileExists "$Transaction\published.lst" 0 rollback_originals
    ClearErrors
    FileOpen $Handle "$Transaction\published.lst" r
    IfErrors rollback_unreadable
    FileSeek $Handle 2 SET
rollback_new:
    ClearErrors
    FileReadUTF16LE $Handle $Entry
    IfErrors rollback_new_done
    ${TrimNewLines} $Entry $Entry
    StrCmp $Entry "" rollback_new
    IfFileExists "$Stage\$Entry" rollback_new
    ${GetParent} "$Stage\$Entry" $Parent
    CreateDirectory "$Parent"
    Rename "$INSTDIR\$Entry" "$Stage\$Entry"
    ${If} ${Errors}
        StrCpy $RollbackFailed 1
    ${EndIf}
    Goto rollback_new
rollback_new_done:
    FileClose $Handle
rollback_originals:
    IfFileExists "$Transaction\entries.lst" 0 rollback_done
    ClearErrors
    FileOpen $Handle "$Transaction\entries.lst" r
    IfErrors rollback_unreadable
    FileSeek $Handle 2 SET
rollback_old:
    ClearErrors
    FileReadUTF16LE $Handle $Entry
    IfErrors rollback_old_done
    ${TrimNewLines} $Entry $Entry
    StrCmp $Entry "" rollback_old
    IfFileExists "$Backup\$Entry" 0 rollback_old
    ${GetParent} "$INSTDIR\$Entry" $Parent
    CreateDirectory "$Parent"
    ClearErrors
    Rename "$Backup\$Entry" "$INSTDIR\$Entry"
    ; Preserve backup evidence if restoration was blocked by another process.
    ${If} ${Errors}
        StrCpy $RollbackFailed 1
    ${EndIf}
    Goto rollback_old
rollback_old_done:
    FileClose $Handle
rollback_done:
    ; Do not discard backups after a failed rollback.
    ${If} $RollbackFailed == 0
        Call CleanTransaction
    ${EndIf}
rollback_return:
    Return
rollback_unreadable:
    ; Keep both journals and backups if their contents cannot be read safely.
    StrCpy $RollbackFailed 1
FunctionEnd

Function CleanTransaction
    StrCmp $Transaction "" clean_return
    IfFileExists "$Transaction\entries.lst" 0 clean_staging
    FileOpen $Handle "$Transaction\entries.lst" r
    FileSeek $Handle 2 SET
clean_next:
    ClearErrors
    FileReadUTF16LE $Handle $Entry
    IfErrors clean_done
    ${TrimNewLines} $Entry $Entry
    StrCmp $Entry "" clean_next
    ${GetParent} "$INSTDIR\$Entry" $Parent
clean_parent:
    StrCmp $Parent "$INSTDIR" clean_next
    RMDir "$Parent"
    ${GetParent} "$Parent" $Parent
    Goto clean_parent
clean_done:
    FileClose $Handle
clean_staging:
    ; Transaction contains only files staged or renamed by this installer.
    RMDir /r "$Transaction"
clean_return:
FunctionEnd

Function .onInit
    SetShellVarContext current
    SetRegView 64
    ${IfNot} ${RunningX64}
        SetErrorLevel 2
        Quit
    ${EndIf}
    ClearErrors
    ReadRegDWORD $2 HKCU "${UNINSTALL_KEY}" "Language"
    ${IfNot} ${Errors}
        StrCpy $LANGUAGE $2
    ${EndIf}
    ${GetParameters} $0
    ClearErrors
    ${GetOptions} $0 "/LANGUAGE=" $3
    ${IfNot} ${Errors}
        StrCpy $LANGUAGE $3
    ${EndIf}
    !insertmacro MUI_LANGDLL_DISPLAY
    Call MapLanguage
    !insertmacro AcquireSetupLock
FunctionEnd

Function MapLanguage
    StrCpy $InitialCulture ""
    ${Switch} $LANGUAGE
        ${Case} ${LANG_ENGLISH}
            StrCpy $InitialCulture "en"
            ${Break}
        ${Case} ${LANG_GERMAN}
            StrCpy $InitialCulture "de"
            ${Break}
        ${Case} ${LANG_FRENCH}
            StrCpy $InitialCulture "fr"
            ${Break}
        ${Case} ${LANG_SPANISH}
            StrCpy $InitialCulture "es"
            ${Break}
        ${Case} ${LANG_ITALIAN}
            StrCpy $InitialCulture "it"
            ${Break}
        ${Case} ${LANG_JAPANESE}
            StrCpy $InitialCulture "ja"
            ${Break}
        ${Case} ${LANG_KOREAN}
            StrCpy $InitialCulture "ko"
            ${Break}
        ${Case} ${LANG_SIMPCHINESE}
            StrCpy $InitialCulture "zh-CN"
            ${Break}
        ${Case} ${LANG_ARABIC}
            StrCpy $InitialCulture "ar"
            ${Break}
        ${Case} ${LANG_TURKISH}
            StrCpy $InitialCulture "tr"
            ${Break}
        ${Case} ${LANG_UKRAINIAN}
            StrCpy $InitialCulture "uk"
            ${Break}
        ${Case} ${LANG_HEBREW}
            StrCpy $InitialCulture "he"
            ${Break}
    ${EndSwitch}
    ${If} $InitialCulture == ""
        SetErrorLevel 2
        Quit
    ${EndIf}
FunctionEnd

Function InitializeUserLanguage
    ReadEnvStr $StateDirectory FLOWERY_STATE_ROOT
    ${GetRoot} "$StateDirectory" $0
    ${If} $0 == ""
        StrCpy $StateDirectory "$LOCALAPPDATA\Flowery.WinUI.Photos"
    ${EndIf}
    CreateDirectory "$StateDirectory"
    ; CREATE_NEW is atomic: never replace even an empty existing preference file.
    System::Call 'kernel32::CreateFileW(w "$StateDirectory\language.state", i 0x40000000, i 1, p 0, i 1, i 0x80, p 0) p.r1'
    StrCmp $1 -1 language_done
    FileWrite $1 "$InitialCulture$\r$\n"
    FileClose $1
language_done:
FunctionEnd

Function un.onInit
    SetShellVarContext current
    SetRegView 64
    ClearErrors
    ReadRegDWORD $2 HKCU "${UNINSTALL_KEY}" "Language"
    ${IfNot} ${Errors}
        StrCpy $LANGUAGE $2
    ${EndIf}
    !insertmacro AcquireSetupLock
FunctionEnd

; Only remove recorded payload files. Never recursively remove an install or settings directory.
Function un.RemoveManifest
    IfFileExists "$Manifest" 0 done
    ClearErrors
    FileOpen $Handle "$Manifest" r
    IfErrors failed
    FileSeek $Handle 2 SET ; UTF-16 BOM
next:
    ClearErrors
    FileReadUTF16LE $Handle $Entry
    IfErrors complete
    ${TrimNewLines} $Entry $Entry
    StrCmp $Entry "" next
    !insertmacro ValidateEntry invalid
    ClearErrors
    Delete "$INSTDIR\$Entry"
    IfErrors invalid
    ${GetParent} "$INSTDIR\$Entry" $Parent
remove_directory:
    StrCmp $Parent "$INSTDIR" next
    RMDir "$Parent" ; Nonempty directories, including user files, are retained.
    ${GetParent} "$Parent" $Parent
    Goto remove_directory
complete:
    FileClose $Handle
    Delete "$Manifest"
    Goto done
invalid:
    FileClose $Handle
failed:
    MessageBox MB_ICONSTOP "$(FileError)" /SD IDOK
    SetErrorLevel 1
    Quit
done:
FunctionEnd

Section "Uninstall"
    StrCpy $Manifest "$INSTDIR\files.lst"
    Call un.RemoveManifest
    Delete "$SMPROGRAMS\Flowery Photos\Flowery Photos.lnk"
    RMDir "$SMPROGRAMS\Flowery Photos"
    DeleteRegKey HKCU "${UNINSTALL_KEY}"
    Delete "$INSTDIR\Uninstall.exe"
    RMDir "$INSTDIR"
SectionEnd
