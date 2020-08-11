import * as functions from 'firebase-functions';
import admin = require('firebase-admin');

admin.initializeApp();

const firestore = admin.firestore();

export const fetchCourts = functions.https.onRequest(async (request, response) => {
    const courtsReference = firestore.collection('Courts');
    const snapshot = await courtsReference.get();
    const data: FirebaseFirestore.DocumentData[] = [];
    snapshot.forEach(async doc => {
        const docData = doc.data();
        data.push({ 'court': docData });
    });
    response.send(data);
});

export const fetchProfiles = functions.https.onRequest(async (request, response) => {
    const profilesReference = firestore.collection('Profiles');
    const snapshot = await profilesReference.get();
    const data: FirebaseFirestore.DocumentData[] = [];
    snapshot.forEach(async doc => {
        const docData = doc.data();
        data.push({ 'profile': docData });
    });
    response.send(data);
});

export const getMyProfile = functions.https.onRequest(async (request, response) => {
    const userId = request.body.userId;
    const userName = request.body.userName;
    const profileReference = firestore.collection('Profiles').doc(userId);
    const doc = await profileReference.get();
    if (doc.data() == undefined) {
        const data = {
            UserID: userId,
            Name: userName,
            Image: '',
            Rank: 0,
            Badges: 0,
            IsMVP: false,
            TeamPosition: 'CENTER',
            CardTopColor: '000000',
            CardBottomColor: '000000',
        }
        const result = await profileReference.set(data);
        if (result != undefined)
            response.send(data);
    } else
        response.send(doc.data());
});