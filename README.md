# HMDClient
Magic Leap 2 Unity client for the FLC Solar Lab. It serves as a generic NOODLES client, providing interaction between the headsets and the broader system infrastructure.

## Requirements
- Magic Leap SDK: can be installed using the _Majic Leap Hub 3_.

## Build an Application

0. Ensure the project Scene Tree (in upper left) is showing MainScene. From the `Project` folder in the lower left corner, select `Scene > MainScene`.
1. **Set ArUco Marker ID**: In Scene Tree, highlight `TrackerRoot`. In the Inspector (right side), change the `Aruco_id` to the corresponding 5x5 ArUco marker ID.
2. **Set IP Address**: In Scene Tree, select `TrackerRoot > RootOffset > NoodlesRoot`. In the Inspector, set the `Server URI`.
3. **Player Settings**: Open `Edit > Project Settings`, on the left, select `Player`.
    - Change **Product Name**: name viable to users.
    - Update **Default Icon**: icon for the application.
    - **Bundle Identifier**: should match common convention: `com.FLC.<Name>`. Name that android uses to identify the application.
4. **Build**: `File > Build Settings`. 
   - **Build**: builds .apk
   - Try **Clean Build** if there are unexpected build errors.
   - **Build and Run** if headset is connected and actively being worn.