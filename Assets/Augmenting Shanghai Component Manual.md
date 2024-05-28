# *Augmenting Shanghai* Component Manual



## 1. Interaction Trigger

Interaction Trigger Prefab locates at Assets/Prefabs

There are three kinds of triggers offered in this script component, including **Collider Trigger**, **Distance Trigger**, **LookAt Trigger**. If you want enable a kind of interaction triggers, remember check the box after "Use XXXXX Trigger".



### 	Collider Trigger

​		Collider Trigger will trigger the Unity Events when the player first time walks into / every time walks into / exit the **Collider with Is Trigger On**.

###### 		Requires Components: Box Collider (or other kinds of Colliders), with **Is Trigger** ***<u>ON</u>***

​	

### 	Distance Trigger

​		Distance Trigger will trigger the Unity Events when the player first time approaches / every time approaches / exit the trigger to a distance.		

#### 		Variable: 

​			Distance: int, the distance that will trigger the event.



### 	LookAt Trigger

​		LookAt Trigger will trigger the Unity Events when the player 1. approaches the trigger to a distance, 2. The player's sight direction is within an angle of the object's position.

​		Event Options include: Player LookAt the trigger for the first time, every time, exit the distance. 

###### 		N.B: Not Look At Trigger is not implemented yet. If you do think it is important, please talk to Shengyang.

#### 		Variable：

​			Look At Angle: The angle range that will count as being looking at.

​			Look At Distance: Only when the player is in the distance range, look At Angle will be calucated.



## 2. Content Prefabs

Content Prefabs locates at Assets/Prefabs/ContentPrefab

### i. 3D Model

​	Change both 1. Mesh Filter -> Mesh to your model's , 2. Material to your models  to use your model.

​	

#### 	Components:

​		Constant Rotate: This script will rotate your object on the XYZ axis at each of the three XYZ speeds.

​			Variable:

​				Rotation Speed: Vector3, the rotational speed of the XYZ axes.

### ii. Audio Player

​	Audio Player can play a music or recording, etc., and provide functions for Interaction Trigger to trigger.

​	There are two kinds of Audio Players with different Presets: 1. Global Audio 2. Spatial Audio

	1. Global Audio can play sound over a large range, so it is suitable for playing background music etc. 
	1.  Spatial Audio can play the sound in a relatively small range. And the volume will be significantly boosted and attenuated within a range depending on the distance.

###### 	N.B: The preset can be changed at Audio Source / 3D Sound Settings

#### 	Components and Functions for Triggers:

​		Audio Content:

​			For Trigger: PlayAudio(), StopAudio(), PlayOneShot()

​		Audio Source:

​			For Trigger: Play(), Pause(), UnPause(), PlayDelay(int secondsToDelay), PlayOnseShot(),

###### 			N.B.: Set the Play On Awake and Loop unchecked if you don't want them to be played on awake or looping.

### iii. Text

​	Show text in the space (no thickness).

#### 	Components and Functions for Triggers:

​		Simple Typewriter: 

​			Inspector: 

​				Content To Type: If you want to use the type writer effect, you need to put the text to type here. The content in the TextMeshPro - Text will be overwritten.

​				Type Speed: The interval time (second) between two characters been typed. 

​				Type Sound: If you want to play sound typewriter sounds (You need to find it yourself :D ), put the audio clip here.

​		Fade Text:

​			Inspector:

​				Fade Duration: Float, the default fade in and fade out time. If you don't change the fade time in the trigger from 0, the value here will be used.

​			For Trigger: 

​				FadeIn(int fadeInTime = 0), FadeOut(int fadeOutTime = 0)



### iv: Video Player

​	It can play the video clip you want by replacing Video Player (component) / Video Clip.

#### 	Functions for Triggers:

​		Video Player: 

​			For Trigger:  Play(), Pause(), Stop()



### v. Sequential Play Manager

​	It can store and trigger a series of Unity Events.

#### 	Components and Functions for Triggers:

​		Sequential Play Manager:

​			Events(numbers of events to be triggered)

​			In each Element:

​				Unity Event ()

​				Interval: Float, the seconds to wait until to trigger the next Element.