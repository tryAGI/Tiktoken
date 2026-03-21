"""Export benchmark input strings to files for cross-language benchmarks.

Run once to generate inputs/ directory. All tokenizer benchmarks read from these files
to ensure identical inputs across languages.
"""

import os

INPUTS = {
    "hello_world": "Hello, World!",
    "multilingual": (
        "The quick brown fox jumps over the lazy dog. "
        "Le renard brun saute par-dessus le chien paresseux. "
        "Ñoño español con eñe. Überraschung auf Deutsch. "
        "Привет мир! Как дела? "
        "素早い茶色の狐が怠惰な犬を飛び越える。"
        "빠른 갈색 여우가 게으른 개를 뛰어넘는다. "
        "مرحبا بالعالم. "
        "สวัสดีชาวโลก "
        "🦊🐕🌍✨"
    ),
    "code": '''import numpy as np
from typing import Optional

def binary_search(arr: list[int], target: int) -> Optional[int]:
    """Find the index of target in a sorted array.

    Args:
        arr: A sorted list of integers.
        target: The value to search for.

    Returns:
        The index of target if found, None otherwise.

    Examples:
        >>> binary_search([1, 3, 5, 7, 9], 5)
        2
        >>> binary_search([1, 3, 5, 7, 9], 4)
        None
    """
    left, right = 0, len(arr) - 1
    while left <= right:
        mid = (left + right) // 2
        if arr[mid] == target:
            return mid
        elif arr[mid] < target:
            left = mid + 1
        else:
            right = mid - 1
    return None

# Performance: O(log n) time, O(1) space
result = binary_search([2, 4, 6, 8, 10, 12], 8)
print(f"Found at index: {result}")  # Output: Found at index: 3''',
    "cjk_heavy": (
        "自然語言處理是人工智能的核心技術之一。大型語言模型的發展推動了文本理解和生成能力的飛速進步。"
        "分詞是中文自然語言處理的基礎任務。由於中文不像英語那樣用空格分隔單詞，因此需要特殊的分詞算法。"
        "日本語の自然言語処理は、ひらがな、カタカナ、漢字の三種類の文字を扱う必要があります。"
        "形態素解析は日本語テキスト処理の最初のステップです。MeCabやJumanなどのツールが広く使われています。"
        "한국어 자연어 처리에서는 형태소 분석이 중요합니다. 한국어는 교착어이므로 하나의 단어에 여러 형태소가 결합됩니다."
        "토큰화는 텍스트를 의미 있는 단위로 분할하는 과정입니다. 바이트 페어 인코딩은 효과적인 서브워드 토큰화 방법입니다."
        "🤖💬🧠🔤✍️📝🌐🗣️💡🔍 "
        "मशीन लर्निंग और प्राकृतिक भाषा प्रसंस्करण ने कंप्यूटर विज्ञान में क्रांति ला दी है। "
        "การประมวลผลภาษาธรรมชาติเป็นสาขาที่สำคัญของปัญญาประดิษฐ์ "
        "معالجة اللغة الطبيعية هي مجال مهم في الذكاء الاصطناعي "
        "עיבוד שפה טבעית הוא תחום חשוב בבינה מלאכותית "
        "ბუნებრივი ენის დამუშავება ხელოვნური ინტელექტის მნიშვნელოვანი სფეროა"
    ),
    "multilingual_long": (
        "The quick brown fox jumps over the lazy dog. This pangram contains every letter of the "
        "English alphabet. It has been used since the late 19th century to test typewriters and keyboards.\n\n"
        "Le renard brun rapide saute par-dessus le chien paresseux. Ce pangramme est utilisé pour tester "
        "les polices de caractères et les claviers. La langue française est parlée par environ 300 millions "
        "de personnes dans le monde.\n\n"
        "Der schnelle braune Fuchs springt über den faulen Hund. Dieser Pangram wird zum Testen von "
        "Schriftarten und Tastaturen verwendet. Die deutsche Sprache hat viele zusammengesetzte Wörter "
        "wie Geschwindigkeitsbegrenzung und Rindfleischetikettierungsüberwachungsaufgabenübertragungsgesetz.\n\n"
        "Быстрая коричневая лиса перепрыгивает через ленивую собаку. Эта фраза используется для проверки "
        "шрифтов и клавиатур. Русский язык является одним из самых распространённых языков в мире. Он "
        "использует кириллический алфавит, который также применяется в украинском, белорусском и болгарском языках.\n\n"
        "素早い茶色の狐が怠惰な犬を飛び越える。この文は日本語のフォントやキーボードのテストに使用されます。"
        "日本語には、ひらがな、カタカナ、漢字の三つの文字体系があります。"
        "自然言語処理の分野では、日本語のトークン化は特に難しい課題です。\n\n"
        "敏捷的棕色狐狸跳过了懒惰的狗。这个句子用于测试字体和键盘。中文是世界上使用人数最多的语言。"
        "中文使用汉字书写系统，每个汉字代表一个音节和一个意义。"
        "自然语言处理领域中，中文分词是一个重要的研究课题。\n\n"
        "빠른 갈색 여우가 게으른 개를 뛰어넘는다. 이 문장은 글꼴과 키보드 테스트에 사용됩니다. "
        "한국어는 한글이라는 고유의 문자 체계를 사용합니다. "
        "한글은 세종대왕이 1443년에 창제한 과학적인 문자입니다.\n\n"
        "الثعلب البني السريع يقفز فوق الكلب الكسول. تُستخدم هذه الجملة لاختبار الخطوط ولوحات المفاتيح. "
        "اللغة العربية هي واحدة من أكثر اللغات انتشاراً في العالم. "
        "تُكتب العربية من اليمين إلى اليسار وتستخدم أبجدية فريدة.\n\n"
        "สุนัขจิ้งจอกสีน้ำตาลกระโดดข้ามสุนัขขี้เกียจ ภาษาไทยมีระบบการเขียนที่ไม่ใช้ช่องว่างระหว่างคำ "
        "การแบ่งคำในภาษาไทยเป็นความท้าทายสำคัญในการประมวลผลภาษาธรรมชาติ\n\n"
        "हिन्दी भाषा देवनागरी लिपि में लिखी जाती है। यह विश्व की चौथी सबसे अधिक बोली जाने वाली भाषा है। "
        "प्राकृतिक भाषा प्रसंस्करण में हिन्दी के लिए विशेष चुनौतियाँ हैं।\n\n"
        "השועל החום המהיר קופץ מעל הכלב העצלן. השפה העברית היא שפה שמית הנכתבת מימין לשמאל. "
        "עיבוד שפה טבעית בעברית מציב אתגרים ייחודיים בשל מורפולוגיה עשירה ומערכת ניקוד.\n\n"
        "სწრაფი ყავისფერი მელა გადახტა ზარმაც ძაღლს. ქართული ენა იყენებს უნიკალურ მხედრულ დამწერლობას. "
        "ბუნებრივი ენის დამუშავება ქართულში განსაკუთრებულ სირთულეებს წარმოადგენს აგლუტინაციური მორფოლოგიის გამო.\n\n"
        "🦊🐕🌍✨ #NLP #multilingual #tokenization"
    ),
}

# Bitcoin whitepaper is too long to inline — read from the shared C# source or use a placeholder
BITCOIN_NOTE = "# Bitcoin whitepaper text: copy from src/benchmarks/Tiktoken.Benchmarks.Shared/Strings.cs"


def main():
    out_dir = os.path.join(os.path.dirname(__file__), "inputs")
    os.makedirs(out_dir, exist_ok=True)

    for name, text in INPUTS.items():
        path = os.path.join(out_dir, f"{name}.txt")
        with open(path, "w", encoding="utf-8") as f:
            f.write(text)
        size = len(text.encode("utf-8"))
        print(f"  {name}: {size:,} bytes -> {path}")

    # Bitcoin: extract from C# source
    cs_path = os.path.join(
        os.path.dirname(__file__), "..", "..",
        "src", "benchmarks", "Tiktoken.Benchmarks.Shared", "Strings.cs"
    )
    if os.path.exists(cs_path):
        with open(cs_path, "r", encoding="utf-8") as f:
            content = f.read()
        # Extract Bitcoin string between @" and ";
        marker = 'public const string Bitcoin = @"'
        start = content.index(marker) + len(marker)
        end = content.index('";', start)
        bitcoin_text = content[start:end].replace('""', '"')
        path = os.path.join(out_dir, "bitcoin.txt")
        with open(path, "w", encoding="utf-8") as f:
            f.write(bitcoin_text)
        size = len(bitcoin_text.encode("utf-8"))
        print(f"  bitcoin: {size:,} bytes -> {path}")
    else:
        print(f"  WARNING: Could not find {cs_path} — skipping bitcoin.txt")

    print("\nDone! Input files ready in inputs/")


if __name__ == "__main__":
    main()
