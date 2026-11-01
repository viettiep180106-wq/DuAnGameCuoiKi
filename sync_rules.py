import os
import shutil

# КОНФИГУРАЦИЯ
# Откуда берем (ваша текущая структура)
SOURCE_DIR = ".cursor/rules"
# Куда кладем (структура Antigravity)
TARGET_DIR = ".agent/rules"

def sync_rules():
    # 1. Создаем целевую папку, если нет
    if not os.path.exists(TARGET_DIR):
        os.makedirs(TARGET_DIR)
        print(f"📁 Создана папка {TARGET_DIR}")

    # 2. Очищаем старые правила в .agent/rules (чтобы удаленные в Cursor файлы удалялись и тут)
    for filename in os.listdir(TARGET_DIR):
        file_path = os.path.join(TARGET_DIR, filename)
        if os.path.isfile(file_path):
            os.remove(file_path)

    # 3. Копируем и переименовываем
    if not os.path.exists(SOURCE_DIR):
        print(f"❌ Ошибка: Не найдена папка источника {SOURCE_DIR}")
        return

    files_count = 0
    for filename in os.listdir(SOURCE_DIR):
        if filename.endswith(".mdc"): # Берем только .mdc
            src_path = os.path.join(SOURCE_DIR, filename)
            
            # Меняем расширение на .md для Antigravity
            new_filename = filename.replace(".mdc", ".md")
            dst_path = os.path.join(TARGET_DIR, new_filename)
            
            # Читаем исходник
            with open(src_path, 'r', encoding='utf-8') as f_src:
                content = f_src.read()
                
            # ТУТ МОЖНО ДОБАВИТЬ АДАПТАЦИЮ КОНТЕНТА
            # Например, Cursor использует frontmatter (--- ... ---), 
            # если Antigravity его не понимает, его можно вырезать или оставить как есть.
            # Пока оставляем как есть, так как Markdown-рендеры обычно скрывают frontmatter.
            
            with open(dst_path, 'w', encoding='utf-8') as f_dst:
                f_dst.write(content)
                
            print(f"✅ Synced: {filename} -> {new_filename}")
            files_count += 1

    print(f"\n🎉 Готово! Синхронизировано правил: {files_count}")

if __name__ == "__main__":
    sync_rules()