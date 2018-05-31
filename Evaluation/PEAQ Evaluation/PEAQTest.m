clear; clc; close all;

%[odg, movb] = PQevalAudio_fn(ref, test)
% 
smallRoomFiles = dir('/Users/jonas/Documents/SMC /Thesis/project_scatAR/scatAR/CODE_BASE/UNITY/findingFirstReflections/recordedAudio/small')
mediumRoomFiles = dir('/Users/jonas/Documents/SMC /Thesis/project_scatAR/scatAR/CODE_BASE/UNITY/findingFirstReflections/recordedAudio/medium')
largeRoomFiles = dir('/Users/jonas/Documents/SMC /Thesis/project_scatAR/scatAR/CODE_BASE/UNITY/findingFirstReflections/recordedAudio/large')
meshRoomFiles = dir('/Users/jonas/Documents/SMC /Thesis/project_scatAR/scatAR/CODE_BASE/UNITY/findingFirstReflections/recordedAudio/mesh')
mediumRoomFiles_Fixed = dir('/Users/jonas/Documents/SMC /Thesis/project_scatAR/scatAR/CODE_BASE/UNITY/findingFirstReflections/recordedAudio/medium_fixed')

extension = '.wav';

%%
newFs = 48000;
destinationS = '/Users/jonas/Documents/SMC /Thesis/PEAQ-Repo/PEAQ/testAudio/resampledSmall/'
destinationMed = '/Users/jonas/Documents/SMC /Thesis/PEAQ-Repo/PEAQ/testAudio/resampledMedium/'
destinationL = '/Users/jonas/Documents/SMC /Thesis/PEAQ-Repo/PEAQ/testAudio/resampledLarge/'
destinationMesh = '/Users/jonas/Documents/SMC /Thesis/PEAQ-Repo/PEAQ/testAudio/resampledMesh/'
destinationMedFix = '/Users/jonas/Documents/SMC /Thesis/PEAQ-Repo/PEAQ/testAudio/resampledMedium_fixed/'

% for k=1:length(smallRoomFiles)
%    file = smallRoomFiles(k).name;
%  
%    if(contains(file, extension) == 1)
%        [x,fs] = audioread(file);
%        [P,Q] = rat(newFs/fs); 
%        xnew = resample(x,P,Q);
%        newFileName = [destinationS,'R',file];
%        
%        audiowrite(newFileName,xnew,newFs);
%    end
% end
% 
% for k=1:length(mediumRoomFiles)
%    file = mediumRoomFiles(k).name;
%  
%    if(contains(file, extension) == 1)
%        [x,fs] = audioread(file);
%        [P,Q] = rat(newFs/fs); 
%        xnew = resample(x,P,Q);
%        newFileName = [destinationMed,'R',file];
%        
%        audiowrite(newFileName,xnew,newFs);
%    end
% end
% 
% 
% for k=1:length(largeRoomFiles)
%    file = largeRoomFiles(k).name;
%  
%    if(contains(file, extension) == 1)
%        [x,fs] = audioread(file);
%        [P,Q] = rat(newFs/fs); 
%        xnew = resample(x,P,Q);
%        newFileName = [destinationL,'R',file];
%        
%        audiowrite(newFileName,xnew,newFs);
%    end
% end
% 
% for k=1:length(meshRoomFiles)
%    file = meshRoomFiles(k).name;
%  
%    if(contains(file, extension) == 1)
%        [x,fs] = audioread(file);
%        [P,Q] = rat(newFs/fs); 
%        xnew = resample(x,P,Q);
%        newFileName = [destinationMesh,'R',file];
%        
%        audiowrite(newFileName,xnew,newFs);
%    end
% end
j = 1;
for k=1:length(mediumRoomFiles_Fixed)
   file = mediumRoomFiles_Fixed(k).name;
 
   if(contains(file, extension) == 1)
       [x,fs] = audioread(file);
       [P,Q] = rat(newFs/fs); 
       xnew = resample(x,P,Q);
       
       %Truncation for IR signals
       if(j < 17)
           for i = 5000 : length(xnew)
                if(xnew(i) > 0 || xnew(i) < 0)
                    truncNew = xnew(i:i+61000-1);
                 break;
                end
           end
       end
       
       %Truncation for ball and speech signals
       if(j > 16)
           for i = 1 : length(xnew)
                if(xnew(i) > 0 || xnew(i) < 0)
                    truncNew = xnew(i:i+110000-1);
                 break;
                end
           end
       end
       
       newFileName = [destinationMedFix,'R',file];
       
       audiowrite(newFileName,truncNew,newFs);
       j = j+1;
   end
end
%soundsc(x,fs);
%soundsc(xnew,48000);

%%
resampledS = dir('/Users/jonas/Documents/SMC /Thesis/PEAQ-Repo/PEAQ/testAudio/resampledSmall/');
resampledMed = dir('/Users/jonas/Documents/SMC /Thesis/PEAQ-Repo/PEAQ/testAudio/resampledMedium/');
resampledL = dir('/Users/jonas/Documents/SMC /Thesis/PEAQ-Repo/PEAQ/testAudio/resampledLarge/');
resampledMesh = dir('/Users/jonas/Documents/SMC /Thesis/PEAQ-Repo/PEAQ/testAudio/resampledMesh/');
resampledMedFix = dir('/Users/jonas/Documents/SMC /Thesis/PEAQ-Repo/PEAQ/testAudio/resampledMedium_fixed/');

% j = 1;
% for i = 4 : 2 : length(resampledS) - 1 
% odgS(j) = PQevalAudio_fn(resampledS(i+1).name, resampledS(i).name);
% j = j+1;
% end
% 
% j = 1;
% for i = 4 : 2 : length(resampledMed) - 1 
% odgMed(j) = PQevalAudio_fn(resampledMed(i+1).name, resampledMed(i).name);
% j = j+1;
% end
% 
% j = 1;
% for i = 4 : 2 : length(resampledL) - 1 
% odgL(j) = PQevalAudio_fn(resampledL(i+1).name, resampledL(i).name);
% j = j+1;
% end
% 
% j = 1;
% for i = 4 : 2 : length(resampledMesh) - 1 
% odgMesh(j) = PQevalAudio_fn(resampledMesh(i+1).name, resampledMesh(i).name);
% j = j+1;
% end

j = 1;
for i = 4 : 2 : length(resampledMedFix) - 1 
odgMedFix(j) = PQevalAudio_fn(resampledMedFix(i+1).name, resampledMedFix(i).name);
j = j+1;
end
%%
%SWAP
for i = 3 : 4: length(odgMedFix)
    %Small
%     a = odgS(i);
%     b = odgS(i+1);
%     
%     odgS(i) = b;
%     odgS(i+1) = a;
%     
%     %Medium
%     a = odgMed(i);
%     b = odgMed(i+1);
%     
%     odgMed(i) = b;
%     odgMed(i+1) = a;
    
    %Medium fixed
    a = odgMedFix(i);
    b = odgMedFix(i+1);
    
    odgMedFix(i) = b;
    odgMedFix(i+1) = a;
    
    %Large
%     a = odgL(i);
%     b = odgL(i+1);
%     
%     odgL(i) = b;
%     odgL(i+1) = a;
%     
%     %Mesh
%     a = odgMesh(i);
%     b = odgMesh(i+1);
%     
%     odgMesh(i) = b;
%     odgMesh(i+1) = a;
end

%%
% 
% medBarIR = [odgMedFix(1) odgMedFix(5); odgMedFix(2) odgMedFix(6); odgMedFix(3) odgMedFix(7); odgMedFix(4) odgMedFix(8)];
% 
% medBarBall = [odgMedFix(9) odgMedFix(13); odgMedFix(10) odgMedFix(14); odgMedFix(11) odgMedFix(15); odgMedFix(12) odgMedFix(16)];
% 
% medBarSpeech = [odgMedFix(17) odgMedFix(21); odgMedFix(18) odgMedFix(22); odgMedFix(19) odgMedFix(23); odgMedFix(20) odgMedFix(24)];
% 
% figure(1);
% bar(medBarIR);
% title('ODG Values for Impulse Response');
% legend('All pass','Low pass');
% ylabel('ODG Value');
% xlabel('Wall absorbtion coefficient');
% xticks([1 2 3 4]);
% xticklabels({'0.80','0.90','0.94','0.97'});
% 
% figure(2);
% bar(medBarBall);
% title('ODG Values for Ball Impact Signal');
% legend('All pass','Low pass');
% ylabel('ODG Value');
% xlabel('Wall absorbtion coefficient');
% xticks([1 2 3 4]);
% xticklabels({'0.80','0.90','0.94','0.97'});
% 
% figure(3);
% bar(medBarSpeech);
% title('ODG Values for Speech Signal');
% legend('All pass','Low pass');
% ylabel('ODG Value');
% xlabel('Wall absorbtion coefficient');
% xticks([1 2 3 4]);
% xticklabels({'0.80','0.90','0.94','0.97'});

medODGIR = [odgMedFix(1:4);odgMedFix(5:8)];
medODGBall = [odgMedFix(9:12);odgMedFix(13:16)];
medODGSpeech = [odgMedFix(17:20);odgMedFix(21:24)];
medODG = [medODGIR;medODGBall;medODGSpeech];

figure(1);
plot(medODGIR','-o');
axis([0.75 4.25 -4 0]);
title('ODG Values for Impulse Response');
legend('All pass','Low pass');
ylabel('ODG Value');
xlabel('Wall reflectance coefficient');
xticks([1 2 3 4]);
xticklabels({'0.80','0.90','0.94','0.97'});
set(gca,'FontSize',14)
grid on;

figure(2);
plot(medODGBall','-o');
axis([0.75 4.25 -4 0]);
title('ODG Values for Ball Impact Signal');
legend('All pass','Low pass');
ylabel('ODG Value');
xlabel('Wall reflectance coefficient');
xticks([1 2 3 4]);
xticklabels({'0.80','0.90','0.94','0.97'});
set(gca,'FontSize',14)
grid on;

figure(3);
plot(medODGSpeech','-o');
axis([0.75 4.25 -4 0]);
title('ODG Values for Speech Signal');
legend('All pass','Low pass');
ylabel('ODG Value');
xlabel('Wall reflectance coefficient');
xticks([1 2 3 4]);
xticklabels({'0.80','0.90','0.94','0.97'});
set(gca,'FontSize',14)
grid on;

figure(4);
plot(medODGIR','-o');
hold on;
plot(medODGBall',':o');
hold on;
plot(medODGSpeech','-.o');
axis([0.75 4.25 -4 0]);
title('ODG Values for different signals');
legend('IR All passed','IR Low pass','Ball All passed','Ball Low pass','Speech All passed','Speech Low pass','Location','southwest');
ylabel('ODG Value');
xlabel('Wall reflectance coefficient');
xticks([1 2 3 4]);
xticklabels({'0.80','0.90','0.94','0.97'});
set(gca,'FontSize',14)
grid on;
%%
refFiles = dir('/Users/jonas/Documents/SMC /Thesis/PEAQ-Repo/PEAQ/testAudio/ref');
testFiles=dir('/Users/jonas/Documents/SMC /Thesis/PEAQ-Repo/PEAQ/testAudio/test');

for k=1:length(refFiles)
   refF = refFiles(k).name;
 
   if(contains(refF, extension) == 1)
       ref = refF;
   end
end

for k=1:length(testFiles)
   test = testFiles(k).name;
   
   if(contains(test, extension) == 1)
        odg(k-3) = PQevalAudio_fn(ref, test)
   end
end


%%
[h, fs] = audioread('RF11_med_080_flat_sdn_IR.wav');
[g, fs] = audioread('RF12_med_080_flat_wgw.wav');

[H, fs] = audioread('F11_med_080_flat_sdn_IR.wav');
[G, fs] = audioread('F12_med_080_flat_wgw.wav');

figure(1);
plot(h);

figure(2);
plot(g);

figure(3);
plot(H);

figure(4);
plot(G);

% for i = 1 : length(h)
%    if(h(i) > 0)
%        truncatedH = h(i:i+110000-1);
%        break;
%    end
% end

%  for i = 1 : length(h)
%     if(h(i) < 0 || h(i) > 0)
%         truncatedG = h(i:i+110000-1);
%         break;
%     end
%  end


