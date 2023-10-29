# Momentum Trading Strategy


Алгоритм работы:

1. Закрытие Бара:
    - вычисляем волотльность (Range) текущего Бара на диапазоне (Окно) N-window, для волотильности используем Персентили.
    - если Бар _низковолотильный_ то пытаемся найти _пограничный_ уровень, если находим _

## Bar Range (Волотильность Bar-а)

Волотильность Bar-а _BarRange_ вычисляется как:
```
    BarRange = (Bar.High - Bar.Low) / 2
```

Glossary:

- TF - TimeFrame Баров с которыми работаем, по умолчанию Дневной (1D).
- N-window - окно расчета волотильности, кол-во Баров которое берем из истории для подсчета вновь сформированного Бара. Например, по умолчанию 90, тогда для TF:1D это 90 дней.
- Персентиль - 
- НизкоВолотильный Бар - Бар который попал в _Персентиль_ ниже PercLow, по умолчанию 25% (0.25).
- ВысокоВолотильный Бар - Бар который попал в _Персентиль_ выше PercHigh, по умолчанию 75% (0.75).
- Граничный уровень - уровень High или Low для _высоковолотильного_ Бара.

Links:
- https://docs.google.com/document/d/1keaCC-mk-6fN0jsWio0CFYmt58HpwNYMVrb9L73APF8
- https://docs.google.com/document/d/16yNCHrlI3-S9n5QjRP5x2nu0RLxytna5WX-OVVTSeVQ
- https://docs.google.com/document/d/11U8RKm8ikh0jB9CWUtSZukWo5nolWmrVCpzdHy_0DDI