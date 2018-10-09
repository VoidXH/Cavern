#ifndef __Vector3_H__
#define __Vector3_H__

#include <math.h>

const float deg2Rad = .0174532924f;
const float rad2Deg = 57.29578f;
const float sqrt2p2 = .7071067811f;
const float sqrt2pm2 = -.7071067811f;

struct Vector3 {
	float x, y, z;
	Vector3(float xin = 0.f, float yin = 0.f, float zin = 0.f);
};

Vector3 placeInSphere(Vector3 angles);
Vector3 placeInCube(Vector3 angles);
inline float lerp(float a, float b, float t);
Vector3 lerp(Vector3 a, Vector3 b, float t);

#endif
