#ifndef __AudioChannel_H__
#define __AudioChannel_H__

#include <iostream> // int32_t without errors
#include <vector>
#include <math.h>
#include "Vector3.h"

class AudioChannel {
	float X, Y; // Spherical position in degrees
	//Vector3 SphericalPos;
	Vector3 CubicalPos;

	void Recalculate();

	static float widthRatio(int Left, int Right, float Pos);
	static float lengthRatio(int Rear, int Front, float Pos);
	static void assignLR(int Channel, int& Left, int& Right, Vector3 Position, Vector3 ChannelPos);
	static void assignHorizontalLayer(int Channel, int& FL, int& FR, int& RL, int& RR, float& ClosestFront, float& ClosestRear,
		Vector3 Position, Vector3 ChannelPos);
	static void fixIncompleteLayer(int& FL, int& FR, int& RL, int& RR);

public:
	static std::vector<AudioChannel> channels;
	bool LFE;

	float getX() { return X; }
	void setX(float val) { X = val; Recalculate(); }
	float getY() { return Y; }
	void setY(float val) { Y = val; Recalculate(); }
	//Vector3 getSphericalPos() { return SphericalPos; }
	Vector3 getCubicalPos() { return CubicalPos; }

	AudioChannel(float iX, float iY);
	AudioChannel(float iX, float iY, bool iLFE);
	static void copy(float* samples, float* output, int64_t sampleCount, int64_t inStep, int64_t outStep, float gain);
	static void render(float* Samples, int32_t sourceChannels, int64_t sampleCount, Vector3 Position, float* output);
	static void renderLFE(float* samples, float lfeGain, int32_t sourceChannels, int64_t sampleCount, float* output);
};

#endif
