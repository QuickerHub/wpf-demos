# JavaScript/C++ 代码被误识别为 CSS 的原因分析

## 问题代码

### JavaScript
```javascript
function add(a, b) {
    return a + b;
}
```

### C++
```cpp
int add(int a, int b) {
    return a + b;
}
```

## 原因分析

### 1. **代码太短，特征不明显**
   - 只有函数定义，缺乏足够的上下文
   - 模型基于 n-gram (bigrams) 特征，需要更多语言特征

### 2. **语法特征重叠**
   - 大括号 `{}` 在 CSS、JavaScript、C++ 中都大量使用
   - CSS 选择器和属性也使用大括号：
     ```css
     .selector {
         property: value;
     }
     ```
   - JavaScript/C++ 函数也使用大括号：
     ```javascript
     function name() {
         // code
     }
     ```

### 3. **缺少语言特有的关键字**
   - JavaScript: 缺少 `console.log`, `var`/`let`/`const`, 箭头函数等
   - C++: 缺少 `#include`, `std::`, `cout`/`cin`, `using namespace` 等

### 4. **`return` 关键字不够独特**
   - `return` 在多种语言中都存在
   - CSS 中虽然没有 `return`，但模型可能看到大括号模式就倾向于 CSS

### 5. **参数列表的相似性**
   - `(a, b)` 这种形式在很多语言中都出现
   - CSS 的某些属性值也可能有类似的括号结构

## 解决方案

### 修改后的代码

#### JavaScript - 添加 console.log
```javascript
function add(a, b) {
    console.log(a + b);
    return a + b;
}
```
- 添加 `console.log` 是 JavaScript 的典型特征
- 增加了更多字符和上下文

#### C++ - 添加 #include
```cpp
#include <iostream>
int add(int a, int b) {
    return a + b;
}
```
- `#include` 是 C++ 预处理指令，非常独特
- 明确表明这是 C++ 代码

## 建议

对于短代码片段，应该：
- **JavaScript**: 包含 `console.log`, `document`, `const`/`let`, 或箭头函数等
- **C++**: 包含 `#include`, `std::`, `cout`/`cin`, 或 `using namespace std` 等
- 使用更明显的语言特有语法和关键字
- 避免只有简单函数定义的情况

