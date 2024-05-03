#include <iostream>


int gob(int *a, int *b) {
	* a = 3;
	* b = 6;

	int num = *b / 2.0;

	/*if (num % 2 == 0) {
		num;
	}

	else if (num % 2 == 1) {
		num = num % 2 + num;
	}*/


	for (int i = 1; i <= num; i++) {
		*a += *a;
	}
	return *a;
}

int main() {
	int x = 0, y = 0;
	printf("x * y = %d\n", gob(&x,&y));
}