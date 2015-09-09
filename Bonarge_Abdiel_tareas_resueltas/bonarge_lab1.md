Despues de Haber Jugado 32 Veces Space Shooter,
puedo Concluir en un patrón que me permitio alcanzar altas puntuaciones.
Analizando al enemigo me percate que el mismo se mueve en sig sag de izquierda a derecha disparando cada un segundo por fila (3 filas)
en el movimiento inicial cuando empieza el juego el enemigo dispara inmediatamente y se mueve en masa hacia la izquierda,
así que el primer movimiento por parte de nosotros debe de ser al lado contrario (derecha), en promedio podría ser (right >= 26), luego el enemigo se mueve a la derecha
asi que nosotros nos movemos a la izquierda podría ser (left de 1 a 7), esto la primera vez cuando el enemigo tiene sus filas completas, disparamos la cantidad de veces
que queramos, el objetivo sería destruir las columnas de enemigos que estén al lado derecho, con esto se iria incrementando el patrón,
el patrón que yo vi fue (right(x cantidad) -> left(x cantidad) ->space(x cantidad) en donde x se va incrementado.
En la carpeta coloque una imagen de una puntuación alta.
Es posible que alla otros patrones utilizando arriba y abajo pero por la forma en que se mueve el enemigo creo que esta podría ser una optima, si el enemigo usara un
movimiento de derecha a izquierda y abajo arriba, el patrón debe de ser totalmente diferente.
