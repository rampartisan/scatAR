close all;
[sdn, fs] = audioread('sdnIR_fixed.wav');
[wgwO, fs] = audioread('wgwIR.wav');
[wgwN, fs] = audioread('wgwIR_fixed3.wav');


%figure(1);
%plot(wgwN,'b');
%hold on;
%plot(sdn,'r');

%figure(2);
%plot(sdn-wgwN);%


sdn = sdn(:,1);
wgwO = wgwO(:,1);
wgwN = wgwN(:,1);

for i = 1 : length(sdn)
   if(sdn(i) > 0.2)
      Tsdn = sdn(i:i+fs-1,1);
      break;
   end
end

%for i = 1 : length(wgwO)
%   if(wgwO(i) > 0.2)
%      TwgwO = wgwO(i:i+fs-1,1);
%      break;
%   end
%end

for i = 1 : length(wgwN)
   if(wgwN(i) > 0.2)
      TwgwN = wgwN(i:i+fs-1,1);
      break;
   end
end

figure(1);
plot(Tsdn,'b');
hold on;
plot(TwgwN,'r');
title('Comparing WGW and SDN')
legend('SDN','WGW');
xlabel('Samples');
ylabel('Amplitude');
set(gca,'FontSize',16)


figure(2);
diffPlot = TwgwN-Tsdn;
plot(diffPlot);
title('Difference between WGW and SDN')
xlabel('Samples');
ylabel('Amplitude');
set(gca,'FontSize',16)


RT60info1 = RT60nofile(Tsdn,fs);
RT60info2 = RT60nofile(TwgwN,fs);

figure(3);
rt60plotting(RT60info1,1);
hold on
rt60plotting(RT60info2,2); 

axis([100 10000 0.5 1.1])
legend('SDN','WGW','Location','southeast');
title('T30 Plot of SDN and WGW');
set(gca,'FontSize',16)